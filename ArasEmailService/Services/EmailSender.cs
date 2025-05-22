using MailKit.Net.Smtp;
using MimeKit;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using ArasEmailService.Models;
using ArasEmailService.Services.Interfaces;
using RazorLight;
using Microsoft.AspNetCore.Html;
using System.Dynamic;

namespace ArasEmailService.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly SmtpClient _smtpClient;
        private readonly ILogger<EmailSender> _logger;
        private readonly IEmailTemplateFactory _emailTemplateFactory;

        public EmailSender(SmtpClient smtpClient, ILogger<EmailSender> logger, IEmailTemplateFactory emailTemplateFactory)
        {
            _smtpClient = smtpClient;
            _logger = logger;
            _emailTemplateFactory = emailTemplateFactory;
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
                message.To.Add(new MailboxAddress($"{booking["customer"]["first_name"]} {booking["customer"]["last_name"]}", booking["customer"]["email"].ToString()));
                message.Bcc.Add(new MailboxAddress("Áras de Staic", "info@arasdestaic.com"));
                message.Subject = $"Booking #{bookingId} Arrival Guide - Áras de Staic";
                message.Body = new TextPart("html") { Text = emailContent };

                // Send the email
                _smtpClient.Send(message);
                _logger.LogInformation($"Arrival guide email sent successfully for booking ID: {bookingId}");

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

    }
}
