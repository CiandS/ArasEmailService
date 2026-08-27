using ArasEmailService.EmailTemplates;
using Microsoft.Extensions.Logging;
using ArasEmailService.Services.Interfaces;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ArasEmailService.Services
{
    public class EmailService
    {
        private readonly IBookingApiClient _apiClient;
        private readonly Services.Interfaces.IBookingStateService _bookingStateService;
        private readonly IEmailSender _emailSender;
        private readonly ILogRepository _logRepository;
        private readonly IEmailTemplateFactory _emailTemplateFactory;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IBookingApiClient apiClient,
            Services.Interfaces.IBookingStateService bookingStateService,
            IEmailSender emailSender,
            ILogRepository logRepository,
            IEmailTemplateFactory emailTemplateFactory,
            ILogger<EmailService> logger)
        {
            _apiClient = apiClient;
            _bookingStateService = bookingStateService;
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

                // Pass only imported bookings to booking state service for UID-based tracking (non-blocking)
                try
                {
                    var importedBookings = bookings.Where(b => b.Value<bool?>("imported") ?? false);
                    if (importedBookings != null && importedBookings.Any())
                    {
                        await _bookingStateService.ProcessBookingStatesAsync(importedBookings);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "BookingStateService failed; continuing processing bookings.");
                }

                foreach (var booking in bookings)
                {
                    int bookingId = (int)booking["id"];

                    // Filter by booking status
                    string status = booking["status"]?.ToString();
                    if (!string.Equals(status, "confirmed", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Skipping booking ID: {BookingId} due to status '{Status}'.", bookingId, status);
                        continue;
                    }

                    // Determine check-in and booking dates early so we can decide late vs arrival-guide
                    DateTimeOffset checkInDateOffset;
                    try
                    {
                        if (!DateTimeOffset.TryParse((string)booking["check_in_date"], CultureInfo.InvariantCulture, out checkInDateOffset))
                        {
                            _logger.LogWarning("Could not parse 'check_in_date' for booking ID: {BookingId}. Skipping.", bookingId);
                            continue;
                        }
                    }
                    catch (Exception)
                    {
                        _logger.LogWarning("Could not parse 'check_in_date' for booking ID: {BookingId}. Skipping.", bookingId);
                        continue;
                    }

                    // Capture raw strings for diagnostics
                    var rawCheckIn = booking["check_in_date"]?.ToString();
                    var rawCreated = booking["date_created"]?.ToString();

                    DateTimeOffset bookingDateOffset = DateTimeOffset.MinValue;
                    try
                    {
                        DateTimeOffset.TryParse((string)booking["date_created"], CultureInfo.InvariantCulture, out bookingDateOffset);
                    }
                    catch (Exception)
                    {
                        _logger.LogWarning("Could not parse 'date_created' for booking ID: {BookingId}. Treating as non-late.", bookingId);
                        bookingDateOffset = DateTimeOffset.MinValue;
                    }

                    // Late booking if booking was created within 10 days before check-in (based on creation vs check-in)
                    bool isLateByAge = false;
                    double diff = double.NaN;
                    if (bookingDateOffset != DateTimeOffset.MinValue)
                    {
                        diff = (checkInDateOffset.UtcDateTime - bookingDateOffset.UtcDateTime).TotalDays;
                        isLateByAge = diff >= 0 && diff <= 10;
                    }

                    // Diagnostic logging to help investigate false positives
                    if (isLateByAge)
                    {
                        _logger.LogInformation("Late booking detected for booking {BookingId}. raw check_in_date='{RawCheckIn}', raw date_created='{RawCreated}', parsed_check_in='{ParsedCheckIn}', parsed_created='{ParsedCreated}', diff_days={Diff}",
                            bookingId,
                            rawCheckIn ?? "(null)",
                            rawCreated ?? "(null)",
                            checkInDateOffset.UtcDateTime.ToString("o"),
                            bookingDateOffset == DateTimeOffset.MinValue ? "(unparsed)" : bookingDateOffset.UtcDateTime.ToString("o"),
                            double.IsNaN(diff) ? (object)"(n/a)" : diff);
                    }

                    // Send the 'late booking' notice when the booking was created within 10 days before check-in
                    if (isLateByAge)
                    {
                        // For late-booking notices we DO NOT exclude imported bookings.
                        bool lateSent = await _logRepository.HasEmailBeenSentAsync(bookingId, DateTime.UtcNow.AddDays(-30), "Late Booking");
                        if (lateSent)
                        {
                            _logger.LogInformation($"Late Booking email already sent for booking ID: {bookingId}.");
                        }
                        else
                        {
                            _logger.LogInformation("Processing booking ID: {BookingId} as late booking (booked within 10 days of check-in).", bookingId);
                            await ProcessLateBookingAsync(booking);
                        }

                        // Do not short-circuit further processing — allow arrival guide flow to continue so the arrival guide
                        // can be sent at its normal time (or immediately if it also qualifies now).
                    }

                    // For arrival guide flow we still exclude imported/external-imported bookings
                    // However, if the booking is late (isLateByAge) we allow arrival guide to be sent as well so
                    // both late booking notice and arrival guide can be delivered when applicable.
                    var isImported = booking.Value<bool?>("imported") ?? false;
                    if (isImported && !isLateByAge)
                    {
                        _logger.LogInformation($"Skipping email for imported booking ID: {bookingId}");
                        continue;
                    }

                    if ((checkInDateOffset.UtcDateTime - DateTime.UtcNow).TotalDays >= 10)
                    {
                        _logger.LogInformation("Skipping booking with ID: {BookingId} as check-in is not 10 days away.", bookingId);
                        continue;
                    }

                    // Before sending arrival guide, check arrival-guide-specific log
                    bool arrivalSent = await _logRepository.HasEmailBeenSentAsync(bookingId, DateTime.UtcNow.AddDays(-30), "Arrival Guide");
                    if (arrivalSent)
                    {
                        _logger.LogInformation($"Arrival Guide already sent for booking ID: {bookingId}. Skipping.");
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
            { 12571, EmailTemplates.BlasketExtensionEmail.Instructions },
            { 12574, EmailTemplates.BlasketExtensionEmail.Instructions },
            { 12573, EmailTemplates.BlasketExtensionEmail.Instructions },
            { 12569, EmailTemplates.BlasketExtensionEmail.Instructions },
            { 12568, EmailTemplates.BlasketExtensionEmail.Instructions },
            { 12570, EmailTemplates.BlasketExtensionEmail.Instructions },
            { 12567, EmailTemplates.BlasketExtensionEmail.Instructions },
            { 12564, EmailTemplates.BlasketExtensionEmail.Instructions },
            { 12565, EmailTemplates.BlasketExtensionEmail.Instructions },
            { 12566, EmailTemplates.BlasketExtensionEmail.Instructions },
        };

        private async Task ProcessBookingAsync(JToken booking)
        {
            try
            {
                int bookingId = (int)booking["id"];

                // Extract the list of instructions
                var instructionsList = new List<HtmlString>();
                var reservedAccommodations = booking["reserved_accommodations"];
                // Track which instruction providers we've already invoked for this booking to avoid duplicate blocks
                var addedProviders = new HashSet<Func<JToken, string>>();
                foreach (var reservedAccommodation in reservedAccommodations)
                {
                    int accommodationId = (int)reservedAccommodation["accommodation"];
                    if (_accommodationInstructions.TryGetValue(accommodationId, out var instructions))
                    {
                        // Only add one block per distinct instruction provider (delegate)
                        if (addedProviders.Contains(instructions))
                            continue;

                        addedProviders.Add(instructions);
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

        private async Task ProcessLateBookingAsync(JToken booking)
        {
            try
            {
                int bookingId = (int)booking["id"];

                // Extract the list of instructions
                var instructionsList = new List<HtmlString>();
                var reservedAccommodations = booking["reserved_accommodations"];
                // Track which instruction providers we've already invoked for this booking to avoid duplicate blocks
                var addedProviders = new HashSet<Func<JToken, string>>();
                foreach (var reservedAccommodation in reservedAccommodations)
                {
                    int accommodationId = (int)reservedAccommodation["accommodation"];
                    if (_accommodationInstructions.TryGetValue(accommodationId, out var instructions))
                    {
                        // Only add one block per distinct instruction provider (delegate)
                        if (addedProviders.Contains(instructions))
                            continue;

                        addedProviders.Add(instructions);
                        // Wrap the string returned by Invoke(booking) into an HtmlString
                        instructionsList.Add(new HtmlString(instructions.Invoke(booking)));
                    }
                }

                try
                {
                    // Attempt to send the late booking email
                    await _emailSender.SendLateBookingEmailAsync(booking, instructionsList);

                    // Log the successful email sending
                    await _logRepository.RecordEmailSentAsync(bookingId, "Late Booking");
                    _logger.LogInformation($"Successfully processed late booking email for booking ID: {bookingId}");
                }
                catch (Exception ex)
                {
                    // Log the failure if the email could not be sent
                    _logger.LogError(ex, $"Failed to send Late Booking email for booking ID: {bookingId}");

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing late booking: {booking["id"]}");
            }
        }
    }
}
