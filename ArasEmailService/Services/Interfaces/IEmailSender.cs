using Microsoft.AspNetCore.Html;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace ArasEmailService.Services.Interfaces
{
    public interface IEmailSender
    {
        /// <summary>
        /// Sends an email message.
        /// </summary>
        /// <param name="emailMessage">The email message to send.</param>
        Task SendArrivalGuideEmailAsync(JToken booking, List<HtmlString> instructionsList);
    }
}
