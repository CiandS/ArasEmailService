using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace ArasEmailService.Services.Interfaces
{
    public interface IEmailTemplateFactory
    {
        /// <summary>
        /// Renders an email template using the provided template path and model.
        /// </summary>
        /// <param name="templatePath">The path to the Razor template.</param>
        /// <param name="model">The data model to bind to the template.</param>
        /// <returns>A rendered email template as a string.</returns>
        Task<string> RenderTemplateAsync(string templatePath, object model);
    }
}
