using MailKit.Net.Smtp;
using MimeKit;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using ArasEmailService.Models;
using ArasEmailService.Services.Interfaces;
using RazorLight;
using Microsoft.AspNetCore.Html;
using System.Dynamic;
using System.Collections.Generic;
using System;
using System.Linq;

namespace ArasEmailService.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly SmtpClient _smtpClient;
        private readonly ILogger<EmailSender> _logger;
        private readonly IEmailTemplateFactory _emailTemplateFactory;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        // Known accommodation id -> friendly display name mapping for late booking emails
        private static readonly System.Collections.Generic.Dictionary<int, string> AccommodationNames =
            new()
            {
                { 66, "Apt. 1: James Joyce" },
                { 104, "Apt. 2: W.B. Yeats" },
                { 100, "Apt. 3: Oscar Wilde" },
                { 1958, "Ste. 4: Eileen Gray" },
                { 98, "Ste. 5: Seamus Heaney" },
                { 102, "Ste. 6: Samuel Beckett" },
                // Blasket extension mapping (use fuller descriptions if desired)
                { 12571, "An Clós (The Courtyard) — Suite 7" },
                { 12574, "An Clós (The Courtyard) — Suite 8" },
                { 12573, "An Clós (The Courtyard) — Suite 9" },
                { 12569, "The Peninsula Collection — Suite 10" },
                { 12568, "The Peninsula Collection — Suite 11" },
                { 12570, "The Peninsula Collection — Suite 12" },
                { 12567, "The Blasket Suite — Suite 13" },
                { 12564, "The Brandon Heights — Suite 14" },
                { 12565, "The Brandon Heights — Suite 15" },
                { 12566, "The Brandon Heights — Suite 16" },
            };

        public EmailSender(SmtpClient smtpClient, ILogger<EmailSender> logger, IEmailTemplateFactory emailTemplateFactory, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _smtpClient = smtpClient;
            _logger = logger;
            _emailTemplateFactory = emailTemplateFactory;
            // Read SMTP settings so we can reconnect if the client becomes disconnected
            _smtpHost = config["Smtp:Host"];
            int.TryParse(config["Smtp:Port"], out _smtpPort);
            _smtpUser = config["Smtp:User"];
            _smtpPass = config["Smtp:Password"];
        }

        public async Task SendArrivalGuideEmailAsync(JToken booking, List<HtmlString> instructionsList)
        {
            int bookingId = (int)booking["id"];
            string directionsLink = "https://maps.app.goo.gl/foZGz3oMBNLWaTNy6";
            string parkingLink = "https://maps.app.goo.gl/bVaQ81k5PjvWeten6";
            string reviewLink = "https://g.page/r/CbQcbuEAJu6REBM/review";

            // Prepare the email model
            var emailModel = new ArrivalGuide
            {
                Id = bookingId,
                CustomerName = $"{booking["customer"]["first_name"]}",
                Instructions = instructionsList,
                DirectionsLink = directionsLink,
                ParkingLink = parkingLink,
                ReviewLink = reviewLink,
            };

            try
            {
                dynamic viewBag = new ExpandoObject();
                viewBag.Raw = new Func<string, HtmlString>(content => new HtmlString(content));
                // Use await on the asynchronous method
                string emailContent = await _emailTemplateFactory.RenderTemplateAsync(
                    "ArasEmailService.EmailTemplates.ArrivalGuideTemplate.cshtml",
                    emailModel);

                // Create the email message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Áras de Staic", "info@arasdestaic.com"));

                var recipientEmail = booking["customer"]?["email"]?.ToString();
                var recipientName = $"{booking["customer"]?["first_name"]} {booking["customer"]?["last_name"]}".Trim();

                if (string.IsNullOrWhiteSpace(recipientEmail))
                {
                    _logger.LogWarning("Skipping Arrival Guide for booking {BookingId} because customer email is missing.", bookingId);
                    return;
                }

                // Validate email address
                try
                {
                    var _ = new System.Net.Mail.MailAddress(recipientEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipping Arrival Guide for booking {BookingId} because customer email is invalid: {Email}", bookingId, recipientEmail);
                    return;
                }

                message.To.Add(new MailboxAddress(recipientName, recipientEmail));
                message.Bcc.Add(new MailboxAddress("Áras de Staic", "info@arasdestaic.com"));
                message.Subject = $"Booking #{bookingId} Arrival Guide - Áras de Staic";
                message.Body = new TextPart("html") { Text = emailContent };

                await SendMessageAsync(message, bookingId);

            }
            catch (TemplateNotFoundException ex)
            {
                _logger.LogError(ex, "Template not found: {TemplatePath}",
                    "ArasEmailService.EmailTemplates.ArrivalGuideTemplate.cshtml");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while rendering the template.");
                throw;
            }
        }

        public async Task SendLateBookingEmailAsync(JToken booking, List<HtmlString> instructionsList)
        {
            int bookingId = (int)booking["id"];

            // Parse minimal fields
            DateTime? checkIn = null;
            DateTime? checkOut = null;
            try
            {
                if (booking["check_in_date"] != null)
                    checkIn = DateTime.Parse((string)booking["check_in_date"]);
                if (booking["check_out_date"] != null)
                    checkOut = DateTime.Parse((string)booking["check_out_date"]);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse checkin/checkout for booking ID: {BookingId}", bookingId);
            }

            var accommodations = new List<string>();
            var reserved = booking["reserved_accommodations"];
            if (reserved != null)
            {
                foreach (var ra in reserved)
                {
                    // Prefer mapping by numeric accommodation id when available
                    int accId = ra["accommodation"]?.Value<int?>() ?? 0;
                    string nameFromFields = ra["accommodation_name"]?.ToString()
                                  ?? ra["accommodation_title"]?.ToString()
                                  ?? ra["title"]?.ToString()
                                  ?? ra["name"]?.ToString();

                    string name = null;

                    if (accId != 0 && AccommodationNames.TryGetValue(accId, out var friendly))
                    {
                        name = friendly;
                    }
                    else
                    {
                        // Fallback to textual fields or numeric id
                        name = nameFromFields ?? (accId != 0 ? accId.ToString() : string.Empty);

                        // Try to extract a suite/room number and append if not present
                        try
                        {
                            var text = name ?? string.Empty;
                            var m = System.Text.RegularExpressions.Regex.Match(text, "(\\d{1,2})");
                            if (m.Success)
                            {
                                var suite = m.Value;
                                if (!text.ToLowerInvariant().Contains("suite") && !text.ToLowerInvariant().Contains("ste") && !text.ToLowerInvariant().Contains("apt"))
                                {
                                    text = $"{text} (Suite {suite})";
                                }
                            }

                            name = text;
                        }
                        catch { }
                    }

                    accommodations.Add(name ?? string.Empty);
                }
            }

            // Use a dedicated LateBooking model (minimal)
            var model = new LateBooking
            {
                Id = bookingId,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                Accommodations = accommodations
            };

            try
            {
                string templatePath = "ArasEmailService.EmailTemplates.LateBookingTemplate.cshtml";
                string emailContent;

                try
                {
                    emailContent = await _emailTemplateFactory.RenderTemplateAsync(templatePath, model);
                }
                catch (TemplateNotFoundException)
                {
                    // fallback to a minimal inline content if template missing
                    emailContent = $@"
                    <p>Check-in: {(model.CheckInDate?.ToString("yyyy-MM-dd") ?? "N/A")}</p>
                    <p>Check-out: {(model.CheckOutDate?.ToString("yyyy-MM-dd") ?? "N/A")}</p>
                    <p>Accommodation(s): {string.Join(", ", model.Accommodations)}</p>";
                    _logger.LogWarning("Late booking template not found; sending minimal inline content for booking ID: {BookingId}", bookingId);
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Áras de Staic", "info@arasdestaic.com"));
                //message.To.Add(new MailboxAddress("Experience Dingle", "info@experiencedingle.ie"));
                message.Bcc.Add(new MailboxAddress("Áras de Staic", "info@arasdestaic.com"));
                message.Subject = $"Booking #{bookingId} — Late booking info";
                message.Body = new TextPart("html") { Text = emailContent };

                await SendMessageAsync(message, bookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending late booking email for booking ID: {BookingId}", bookingId);
                throw;
            }
        }

        private async Task SendMessageAsync(MimeMessage message, int bookingId)
        {
            try
            {
                if (!_smtpClient.IsConnected)
                {
                    _logger.LogInformation("SMTP client not connected. Attempting to reconnect...");
                    if (!string.IsNullOrWhiteSpace(_smtpHost) && _smtpPort != 0)
                    {
                        _smtpClient.Connect(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                        if (!string.IsNullOrWhiteSpace(_smtpUser))
                            _smtpClient.Authenticate(_smtpUser, _smtpPass);
                    }
                    else
                    {
                        _logger.LogWarning("SMTP host/port not available in configuration; cannot reconnect.");
                    }
                }

                // Final safeguard: ensure at least one recipient exists
                if (!message.To.Any() && !message.Cc.Any() && !message.Bcc.Any())
                {
                    _logger.LogWarning("Skipping send for booking {BookingId}: message has no recipients.", bookingId);
                    return;
                }

                _smtpClient.Send(message);
                _logger.LogInformation("Email sent successfully for booking {BookingId} to recipients: {To}", bookingId, string.Join(',', message.To.Select(a => a.ToString())));
            }
            catch (MailKit.Net.Smtp.SmtpCommandException sxe)
            {
                _logger.LogError(sxe, "SMTP command failed sending booking {BookingId}: StatusCode={StatusCode}, Message={Message}", bookingId, sxe.StatusCode, sxe.Message);
                throw;
            }
            catch (MailKit.ServiceNotConnectedException snc)
            {
                _logger.LogError(snc, "SMTP client not connected when sending booking {BookingId}", bookingId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending email for booking {BookingId}", bookingId);
                throw;
            }
        }
    }
}
