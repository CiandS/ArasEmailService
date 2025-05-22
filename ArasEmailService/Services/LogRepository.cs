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

        public async Task<bool> HasEmailBeenSentAsync(int bookingId, DateTime startDate)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"SELECT COUNT(1) FROM grN_EmailLog 
                    WHERE booking_id = @bookingId AND email_sent_date >= @startDate";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@bookingId", bookingId);
                        command.Parameters.AddWithValue("@startDate", startDate);

                        int count = Convert.ToInt32(await command.ExecuteScalarAsync());

                        // Set the emailSent flag to true if the email was found to be sent
                        bool emailSent = count > 0;

                        _logger.LogInformation($"Checked email sent status for booking ID: {bookingId}. Email sent: {emailSent}");
                        return emailSent;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email sent status for booking ID: {BookingId}", bookingId);
                return false;  // Return false if there's an error
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
                        _logger.LogInformation("Arrival guide email log recorded for booking ID: {BookingId}", bookingId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording arrival guide log for booking ID: {BookingId}", bookingId);
            }
        }
    }
}
