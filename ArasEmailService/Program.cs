using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using ArasEmailService.Services;

namespace ArasEmailService
{
    class Program
    {
        static void Main(string[] args)
        {
            // Build the configuration from appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            IConfiguration config = builder.Build();

            // Set up Dependency Injection
            var services = new ServiceCollection();

            services.AddTransient<SmtpClient>(provider =>
            {
                var smtpClient = new SmtpClient();
                smtpClient.Connect(config["Smtp:Host"], int.Parse(config["Smtp:Port"]), SecureSocketOptions.StartTls);
                smtpClient.Authenticate(config["Smtp:User"], config["Smtp:Password"]);
                return smtpClient;
            });

            services.AddTransient<EmailService>();

            var serviceProvider = services.BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                try
                {
                    emailService.ProcessBookings();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred: {ex.Message}");
                }
            }

            Console.WriteLine("Booking processing completed.");
        }
    }
}
