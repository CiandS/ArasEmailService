using System;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using MimeKit;
using MailKit.Net.Smtp;
using System.Text;
using ArasEmailService.EmailTemplates;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using ArasEmailService.Models;
using Microsoft.Extensions.Configuration;

namespace ArasEmailService.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly string apiUrl;
        private readonly string username;
        private readonly string encryptedPassword;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;

            // Initialize the fields using the configuration
            connectionString = _configuration["Database:ConnectionString"];
            apiUrl = _configuration["Api:Url"];
            username = _configuration["Api:Username"];
            encryptedPassword = _configuration["Api:EncryptedPassword"];
        }
        private readonly SmtpClient _smtpClient;
        private readonly EmailTemplateFactory _emailTemplateFactory;

        public EmailService(SmtpClient smtpClient)
        {
            _smtpClient = smtpClient;
        }

        public EmailService(EmailTemplateFactory emailTemplateFactory)
        {
            _emailTemplateFactory = emailTemplateFactory;
        }

        public JToken GetBookingData()
        {
            using (var client = new HttpClient())
            {
                var byteArray = Encoding.ASCII.GetBytes($"{username}:{encryptedPassword}");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var response = client.GetAsync(apiUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync().Result;
                    return JToken.Parse(data); // This parses the JSON response into a JToken
                }
                else
                {
                    Console.WriteLine("Failed to retrieve data. Status Code: " + response.StatusCode);
                    return null;
                }
            }
        }

        public bool HasEmailBeenSent(int bookingId, DateTime startDate, ref int totalSent)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT COUNT(1) FROM staging_grN_EmailLog 
                         WHERE booking_id = @bookingId AND email_sent_date >= @startDate";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@bookingId", bookingId);
                    command.Parameters.AddWithValue("@startDate", startDate);

                    int count = Convert.ToInt32(command.ExecuteScalar());
                    totalSent += count;  // Accumulate based on filtered results
                    return count > 0;
                }
            }
        }
        public int GetTotalEmailsSent()
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(1) FROM staging_grN_EmailLog";
                using (var command = new MySqlCommand(query, connection))
                {
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public void RecordEmailSent(int bookingId, string emailContent)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string insertQuery = "INSERT INTO staging_grN_EmailLog (booking_id, email_sent_date, content) VALUES (@bookingId, @dateSent, @content)";
                using (var command = new MySqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@bookingId", bookingId);
                    command.Parameters.AddWithValue("@dateSent", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@content", emailContent);
                    command.ExecuteNonQuery();
                }
            }
        }

        private readonly Dictionary<int, string> _accommodationTemplates = new Dictionary<int, string>
        {
            { 66, "Apt1JamesJoyce" },
            { 104, "Apt2WBYeats" },
            { 100, "Apt3OscarWilde" },
            { 1958, "No4EileenGray" },
            { 98, "No5SeamusHeaney" },
            { 102, "No6SamuelBeckett" },

        };

        public void SendArrivalGuideEmail(JToken booking)
        {
            int bookingId = (int)booking["id"];

            foreach (var reservedAccommodation in booking["reserved_accommodations"])
            {
                // Get the accommodation ID
                int accommodationId = (int)reservedAccommodation["accommodation"];

                // Customize the email template based on accommodationId
                string templateName = accommodationId switch
                {
                    66 => "Apt1JamesJoyce",
                    104 => "Apt2WBYeats",
                    100 => "Apt3OscarWilde",
                    1958 => "No4EileenGray",
                    98 => "No5SeamusHeaney",
                    102 => "No6SamuelBeckett",
                    _ => "DefaultArrivalGuideEmail" // Fallback template
                };

                var emailModel = new ArrivalGuide
                {
                    Id = (int)booking["id"],
                    CustomerName = $"{booking["customer"]["first_name"]} {booking["customer"]["last_name"]}",
                    DirectionsLink = $"https://maps.app.goo.gl/foZGz3oMBNLWaTNy6",
                    ParkingLink = "https://maps.app.goo.gl/bVaQ81k5PjvWeten6",
                };

                // Render the appropriate Razor view to a string
                string emailContent = _emailTemplateFactory.RenderRazorViewToString(templateName, emailModel);

                // Send the email
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Áras de Staic", "info@arasdestaic.com"));
                message.To.Add(new MailboxAddress($"{booking["customer"]["first_name"]} {booking["customer"]["last_name"]}", booking["customer"]["email"].ToString()));
                message.Bcc.Add(new MailboxAddress("Áras de Staic", "info@arasdestaic.com"));
                message.Subject = $"Booking {booking["id"]} Arrival Guide - Áras de Staic";
                message.Body = new TextPart("html") { Text = emailContent };

                try
                {
                    _smtpClient.Send(message);
                    Console.WriteLine($"Email sent successfully for accommodation ID: {accommodationId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send email for accommodation ID {accommodationId}: {ex.Message}");
                }
            }
        }

            public void ProcessBookings()
        {
            JToken bookings = GetBookingData();
            if (bookings != null && bookings.Any())
            {
                foreach (var booking in bookings)
                {
                    SendArrivalGuideEmail(booking);
                }
            }
            else
            {
                Console.WriteLine("No new bookings.");
            }
        }
    }
}
