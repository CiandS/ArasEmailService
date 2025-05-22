using ArasEmailService.EmailTemplates;
using Microsoft.Extensions.Logging;
using ArasEmailService.Services.Interfaces;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Html;

namespace ArasEmailService.Services
{
    public class EmailService
    {
        private readonly IBookingApiClient _apiClient;
        private readonly IEmailSender _emailSender;
        private readonly ILogRepository _logRepository;
        private readonly IEmailTemplateFactory _emailTemplateFactory;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IBookingApiClient apiClient,
            IEmailSender emailSender,
            ILogRepository logRepository,
            IEmailTemplateFactory emailTemplateFactory,
            ILogger<EmailService> logger)
        {
            _apiClient = apiClient;
            _emailSender = emailSender;
            _logRepository = logRepository;
            _emailTemplateFactory = emailTemplateFactory;
            _logger = logger;
        }

        public async Task ProcessBookingsAsync()
        {
            try
            {
                var bookings = await _apiClient.GetBookingDataAsync();
                if (bookings == null || !bookings.Any())
                {
                    _logger.LogInformation("No bookings to process. \n");
                    return;
                }

                foreach (var booking in bookings)
                {
                    int bookingId = (int)booking["id"];

                    // Check if the booking is imported
                    var isImported = booking.Value<bool?>("imported") ?? false;
                    if (isImported)
                    {
                        _logger.LogInformation($"Skipping email for imported booking ID: {bookingId}");
                        continue;
                    }

                    // Check if the email has been sent in the last 30 days
                    bool emailSent = await _logRepository.HasEmailBeenSentAsync(bookingId, DateTime.UtcNow.AddDays(-30));
                    if (emailSent)
                    {
                        _logger.LogInformation($"Email already sent for booking ID: {bookingId}. Skipping.");
                        continue;
                    }

                    var checkInDate = DateTime.Parse((string)booking["check_in_date"]);
                    if ((checkInDate - DateTime.UtcNow).TotalDays >= 10)
                    {
                        _logger.LogInformation("Skipping booking with ID: {BookingId} as check-in is not 10 days away.", bookingId);
                        continue;
                    }

                    // Process the booking (e.g., send the email)
                    await ProcessBookingAsync(booking);
                }

                _logger.LogInformation("Finished processing bookings. \n");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing bookings. \n");
            }
        }

        private readonly Dictionary<int, Func<JToken, string>> _accommodationInstructions = new()
        {
            { 66, Apt1JamesJoyceEmail.Instructions },
            { 104, Apt2WBYeatsEmail.Instructions },
            { 100, Apt3OscarWildeEmail.Instructions },
            { 1958, Ste4EileenGrayEmail.Instructions },
            { 98, Ste5SeamusHeaneyEmail.Instructions },
            { 102, Ste6SamuelBeckettEmail.Instructions },
        };

        private async Task ProcessBookingAsync(JToken booking)
        {
            try
            {
                int bookingId = (int)booking["id"];

                // Extract the list of instructions
                var instructionsList = new List<HtmlString>();
                var reservedAccommodations = booking["reserved_accommodations"];
                foreach (var reservedAccommodation in reservedAccommodations)
                {
                    int accommodationId = (int)reservedAccommodation["accommodation"];
                    if (_accommodationInstructions.TryGetValue(accommodationId, out var instructions))
                    {
                        // Wrap the string returned by Invoke(booking) into an HtmlString
                        instructionsList.Add(new HtmlString(instructions.Invoke(booking)));
                    }
                }


                try
                {
                    // Attempt to send the email
                    await _emailSender.SendArrivalGuideEmailAsync(booking, instructionsList);

                    // Log the successful email sending
                    await _logRepository.RecordEmailSentAsync(bookingId, "Arrival Guide");
                    _logger.LogInformation($"Successfully processed email for booking ID: {bookingId}");
                }
                catch (Exception ex)
                {
                    // Log the failure if the email could not be sent
                    _logger.LogError(ex, $"Failed to send Arrival Guide email for booking ID: {bookingId}");

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing booking: {booking["id"]}");
            }
        }
    }
}
