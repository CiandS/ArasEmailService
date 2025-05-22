using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using RazorLight;
using ArasEmailService.Models;
using ArasEmailService.Services;
using ArasEmailService.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ArasEmailService.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            var apiConfig = config.GetSection("Api").Get<Api>();
            services.Configure<Api>(config.GetSection("Api"));

            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<Api>>().Value);
            services.AddHttpClient<IBookingApiClient, BookingApiClient>();

            // Register logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            // Register SmtpClient
            services.AddTransient<SmtpClient>(provider =>
            {
                var smtpClient = new SmtpClient();
                smtpClient.Connect(config["Smtp:Host"], int.Parse(config["Smtp:Port"]), MailKit.Security.SecureSocketOptions.StartTls);
                smtpClient.Authenticate(config["Smtp:User"], config["Smtp:Password"]);
                return smtpClient;
            });

            // Register RazorLight engine
            services.AddSingleton<IRazorLightEngine>(new RazorLightEngineBuilder()
                .UseEmbeddedResourcesProject(typeof(Program).Assembly) // Use the assembly where the resource is embedded
                .EnableDebugMode() // Enable debug mode for further insights
                .UseMemoryCachingProvider()
                .Build());

            // Register application services with interfaces
            services.AddSingleton<IBookingApiClient, BookingApiClient>();
            services.AddSingleton<IEmailTemplateFactory, EmailTemplateFactory>();
            services.AddSingleton<IEmailSender, EmailSender>();
            services.AddSingleton<EmailService>();

            // Register LogRepository with connection string and logger
            services.AddScoped<ILogRepository>(provider =>
            {
                var connectionString = config.GetValue<string>("Database:ConnectionString");
                var logger = provider.GetRequiredService<ILogger<LogRepository>>();
                return new LogRepository(connectionString, logger);
            });

            return services;
        }
    }
}
