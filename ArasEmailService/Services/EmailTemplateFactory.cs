using ArasEmailService.Models;
using ArasEmailService.Services.Interfaces;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Logging;
using RazorLight;

namespace ArasEmailService.Services
{
    public class EmailTemplateFactory : IEmailTemplateFactory
    {
        private readonly IRazorLightEngine _razorLightEngine;
        private readonly ILogger<EmailTemplateFactory> _logger;

        public EmailTemplateFactory(IRazorLightEngine razorLightEngine, ILogger<EmailTemplateFactory> logger)
        {
            _razorLightEngine = razorLightEngine ?? throw new ArgumentNullException(nameof(razorLightEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> RenderTemplateAsync(string templatePath, object model)
        {
            if (string.IsNullOrWhiteSpace(templatePath))
            {
                _logger.LogError("Template path is null or empty.");
                throw new ArgumentException("Template path cannot be null or empty.", nameof(templatePath));
            }

            try
            {
                _logger.LogInformation("Rendering template: {TemplatePath} with model: {ModelType}", templatePath, model?.GetType().Name ?? "null");

                // Preprocess instructions to HtmlString
                if (model is ArrivalGuide arrivalGuideModel)
                {
                    // Preprocess instructions to HtmlString
                    arrivalGuideModel.Instructions = arrivalGuideModel.Instructions
                        .Select(instruction => new HtmlString(instruction?.ToString() ?? string.Empty))
                        .ToList();
                }

                // Render the template
                var template = await _razorLightEngine.CompileRenderAsync(templatePath, model);

                _logger.LogInformation("Template rendered successfully: {TemplatePath}", templatePath);

                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while rendering template: {TemplatePath}", templatePath);
                throw;
            }
        }
    }
}
