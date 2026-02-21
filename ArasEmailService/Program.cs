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
            // Build the configuration from appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true); // optional flag on json for env variables in github
            IConfiguration config = builder.Build();

            // Set up Dependency Injection
            var services = new ServiceCollection();
            services.AddApplicationServices(config);  

            var serviceProvider = services.BuildServiceProvider();

            // Get the logger for Program class
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Starting booking processing...");

                // Create a scope and get the EmailService
                using (var scope = serviceProvider.CreateScope())
                {
                    var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                    // Call the ProcessBookings method
                    await emailService.ProcessBookingsAsync();

                    logger.LogInformation("Booking processing completed.");
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                logger.LogError(ex, "An error occurred during booking processing.");
            }
        }
    }
}
