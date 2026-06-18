using ArasEmailService.Services.Interfaces;
using ArasEmailService.Models;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ArasEmailService.Services
{
    public class BookingStateService : IBookingStateService
    {
        private readonly string _connectionString;
        private readonly ILogger<BookingStateService> _logger;

        public BookingStateService(string connectionString, ILogger<BookingStateService> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger;
        }

        public async Task ProcessBookingStatesAsync(IEnumerable<JToken> bookings)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Ensure table exists
                await EnsureTableExistsAsync(connection);

                foreach (var booking in bookings)
                {
                    try
                    {
                        string uid = booking["uid"]?.ToString() ?? booking["id"]?.ToString();
                        if (string.IsNullOrWhiteSpace(uid))
                        {
                            _logger.LogWarning("Booking missing UID and ID - skipping state processing.");
                            continue;
                        }

                        DateTime checkIn = DateTime.MinValue;
                        DateTime checkOut = DateTime.MinValue;
                        try { if (booking["check_in_date"] != null) checkIn = DateTime.Parse((string)booking["check_in_date"]); } catch { }
                        try { if (booking["check_out_date"] != null) checkOut = DateTime.Parse((string)booking["check_out_date"]); } catch { }

                        int accommodationId = 0;
                        var reserved = booking["reserved_accommodations"];
                        if (reserved != null)
                        {
                            foreach (var ra in reserved)
                            {
                                accommodationId = ra.Value<int?>("accommodation") ?? accommodationId;
                                break; // take first
                            }
                        }

                        string guestName = "";
                        try { guestName = booking["customer"]?["first_name"]?.ToString() + " " + booking["customer"]?["last_name"]?.ToString(); } catch { }

                        var model = new BookingStateModel
                        {
                            UID = uid,
                            CheckIn = checkIn,
                            CheckOut = checkOut,
                            AccommodationId = accommodationId,
                            GuestName = guestName,
                        };

                        model.SnapshotHash = CalculateSnapshotHash(model);

                        await UpsertStateAsync(connection, model, booking);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed processing booking state for booking: {Booking}", booking);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing booking states");
            }
        }

        private async Task EnsureTableExistsAsync(MySqlConnection connection)
        {
            var createSql = @"
CREATE TABLE IF NOT EXISTS grN_BookingUidLog (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    uid VARCHAR(255) NOT NULL,
    first_seen_utc DATETIME NOT NULL,
    last_seen_utc DATETIME NOT NULL,
    snapshot_hash VARCHAR(255) NOT NULL,
    late_booking_flag TINYINT(1) DEFAULT 0,
    modified_flag TINYINT(1) DEFAULT 0,
    accommodation_id INT NULL,
    guest_name VARCHAR(512) NULL,
    UNIQUE KEY ux_uid (uid)
) ENGINE=InnoDB;";

            using var cmd = new MySqlCommand(createSql, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        private string CalculateSnapshotHash(BookingStateModel model)
        {
            var input = $"{model.CheckIn:yyyy-MM-dd}|{model.CheckOut:yyyy-MM-dd}|{model.AccommodationId}|{model.GuestName}";
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private async Task UpsertStateAsync(MySqlConnection connection, BookingStateModel model, JToken booking)
        {
            // Check existing
            string selectSql = "SELECT id, snapshot_hash, first_seen_utc FROM grN_BookingUidLog WHERE uid = @uid LIMIT 1";
            using var selectCmd = new MySqlCommand(selectSql, connection);
            selectCmd.Parameters.AddWithValue("@uid", model.UID);

            using var reader = await selectCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var id = Convert.ToInt64(reader["id"]);
                var existingHash = reader.IsDBNull(1) ? null : reader.GetString(1);
                var firstSeen = reader.IsDBNull(2) ? DateTime.MinValue : reader.GetDateTime(2);
                await reader.CloseAsync();

                bool modified = existingHash != model.SnapshotHash;

                string updateSql = @"UPDATE grN_BookingUidLog
SET last_seen_utc = @lastSeen, snapshot_hash = @snapshot, modified_flag = @modified, accommodation_id = @accId, guest_name = @guest
WHERE id = @id";

                using var updateCmd = new MySqlCommand(updateSql, connection);
                updateCmd.Parameters.AddWithValue("@lastSeen", DateTime.UtcNow);
                updateCmd.Parameters.AddWithValue("@snapshot", model.SnapshotHash);
                updateCmd.Parameters.AddWithValue("@modified", modified ? 1 : 0);
                updateCmd.Parameters.AddWithValue("@accId", model.AccommodationId == 0 ? (object)DBNull.Value : model.AccommodationId);
                updateCmd.Parameters.AddWithValue("@guest", string.IsNullOrWhiteSpace(model.GuestName) ? (object)DBNull.Value : model.GuestName);
                updateCmd.Parameters.AddWithValue("@id", id);

                await updateCmd.ExecuteNonQueryAsync();

                // update late flag if needed
                await UpdateLateFlagIfNeeded(connection, id, model, booking, firstSeen);
            }
            else
            {
                await reader.CloseAsync();

                string insertSql = @"INSERT INTO grN_BookingUidLog (uid, first_seen_utc, last_seen_utc, snapshot_hash, late_booking_flag, modified_flag, accommodation_id, guest_name)
VALUES (@uid, @firstSeen, @lastSeen, @snapshot, @lateFlag, @modified, @accId, @guest)";

                bool lateFlag = DetermineLateFlag(model, booking, DateTime.UtcNow);

                using var insertCmd = new MySqlCommand(insertSql, connection);
                insertCmd.Parameters.AddWithValue("@uid", model.UID);
                insertCmd.Parameters.AddWithValue("@firstSeen", DateTime.UtcNow);
                insertCmd.Parameters.AddWithValue("@lastSeen", DateTime.UtcNow);
                insertCmd.Parameters.AddWithValue("@snapshot", model.SnapshotHash);
                insertCmd.Parameters.AddWithValue("@lateFlag", lateFlag ? 1 : 0);
                insertCmd.Parameters.AddWithValue("@modified", 0);
                insertCmd.Parameters.AddWithValue("@accId", model.AccommodationId == 0 ? (object)DBNull.Value : model.AccommodationId);
                insertCmd.Parameters.AddWithValue("@guest", string.IsNullOrWhiteSpace(model.GuestName) ? (object)DBNull.Value : model.GuestName);

                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        private async Task UpdateLateFlagIfNeeded(MySqlConnection connection, long id, BookingStateModel model, JToken booking, DateTime firstSeen)
        {
            bool lateFlag = DetermineLateFlag(model, booking, firstSeen);
            string sql = "UPDATE grN_BookingUidLog SET late_booking_flag = @late WHERE id = @id";
            using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@late", lateFlag ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        private bool DetermineLateFlag(BookingStateModel model, JToken booking, DateTime firstSeenUtc)
        {
            // Late booking: check-in within 72 hours AND FirstSeenUtc within 24 hours
            if (model.CheckIn == DateTime.MinValue)
                return false;

            var now = DateTime.UtcNow;
            var hoursToCheckin = (model.CheckIn - now).TotalHours;
            var hoursSinceFirstSeen = (now - firstSeenUtc).TotalHours;

            return hoursToCheckin <= 72 && hoursSinceFirstSeen <= 24;
        }
    }
}
