using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ArasEmailService.Infrastructure.Extensions;
using ArasEmailService.Services;

namespace ArasEmailService
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Build configuration safely
            var builder = new ConfigurationBuilder();

            if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == null &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
            {
                // Local dev only: specify exact path to JSON
                var jsonPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "appsettings.json");
                if (File.Exists(jsonPath))
                {
                    builder.AddJsonFile(jsonPath, optional: true, reloadOnChange: true);
                }
            }

            // Always load env vars (GitHub secrets)
            builder.AddEnvironmentVariables();
            var config = builder.Build();

            // DI setup
            var services = new ServiceCollection();
            services.AddApplicationServices(config);
            var serviceProvider = services.BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Starting booking processing...");

                using var scope = serviceProvider.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
                await emailService.ProcessBookingsAsync();

                logger.LogInformation("Booking processing completed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during booking processing.");
                throw; // make sure GitHub Actions sees the failure
            }
        }
    }
}
