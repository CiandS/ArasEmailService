using ArasEmailService.Services.Interfaces;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

namespace ArasEmailService.Services
{
    public class LogRepository : ILogRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<LogRepository> _logger;

        public LogRepository(string connectionString, ILogger<LogRepository> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger;
        }

        public async Task<bool> HasEmailBeenSentAsync(int bookingId, DateTime startDate, string emailType = null)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"SELECT COUNT(1) FROM grN_EmailLog 
                    WHERE booking_id = @bookingId AND email_sent_date >= @startDate";

                    if (!string.IsNullOrWhiteSpace(emailType))
                    {
                        query += " AND email_type = @type";
                    }

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@bookingId", bookingId);
                        command.Parameters.AddWithValue("@startDate", startDate);

                        if (!string.IsNullOrWhiteSpace(emailType))
                        {
                            command.Parameters.AddWithValue("@type", emailType);
                        }

                        int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                        bool emailSent = count > 0;

                        _logger.LogInformation("Checked email sent status for booking ID: {BookingId}, emailType: {EmailType}. Email sent: {EmailSent}", bookingId, emailType ?? "(any)", emailSent);
                        return emailSent;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email sent status for booking ID: {BookingId}", bookingId);
                return false;
            }
        }

        public async Task RecordEmailSentAsync(int bookingId, string emailType)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string insertQuery = @"
                INSERT INTO grN_EmailLog (booking_id, email_sent_date, email_type) 
                VALUES (@bookingId, @dateSent, @type)";

                    using (var command = new MySqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@bookingId", bookingId);
                        command.Parameters.AddWithValue("@dateSent", DateTime.UtcNow);
                        command.Parameters.AddWithValue("@type", emailType);

                        await command.ExecuteNonQueryAsync();
                        _logger.LogInformation("Email log recorded for booking ID: {BookingId}, emailType: {EmailType}", bookingId, emailType);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording email log for booking ID: {BookingId}", bookingId);
            }
        }
    }
}
