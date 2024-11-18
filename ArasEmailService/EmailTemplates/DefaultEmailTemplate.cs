using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArasEmailService.EmailTemplates
{
    public class DefaultEmailTemplate
    {
        public static string GenerateEmailBody(JToken booking)
        {
            return "<h1>Thank you for your booking!</h1>" +
                   $"<p>Dear {booking["customer"]["first_name"]},</p>" +
                   "<p>We’re confirming your booking with the following details:</p>" +
                   $"<p>Booking ID: {booking["id"]}<br>" +
                   $"Room: {booking["room"]}<br>" +
                   $"Check-in: {booking["check_in"]}<br>" +
                   $"Check-out: {booking["check_out"]}</p>" +
                   "<p>We look forward to your stay!</p>";
        }
    }
}
