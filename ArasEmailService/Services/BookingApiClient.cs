using ArasEmailService.Models;
using ArasEmailService.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ArasEmailService.Services
{
    public class BookingApiClient : IBookingApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _username;
        private readonly string _encryptedPassword;
        private readonly ILogger<BookingApiClient> _logger;
        private readonly Api _settings;

        public BookingApiClient(HttpClient httpClient, Api settings, ILogger<BookingApiClient> logger)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrWhiteSpace(settings.Url))
                throw new ArgumentNullException(nameof(settings.Url));

            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _username = settings.Username;
            _encryptedPassword = settings.EncryptedPassword;
            _settings = settings;
            _logger = logger;

            _httpClient.BaseAddress = new Uri(settings.Url); // Set base address from config
            _logger.LogInformation("BookingApiClient initialized with base URL: {Url}", _httpClient.BaseAddress);
        }

        public async Task<JToken> GetBookingDataAsync(DateTime? afterDate = null, int page = 1, int perPage = 100)
        {
            try
            {
                DateTime startDate = DateTime.Now;
                DateTime endDate = DateTime.Now.AddDays(11);
                string formattedStartDate = startDate.ToString("yyyy-MM-dd"); // Note: use hyphens
                string formattedEndDate = endDate.ToString("yyyy-MM-dd");

                // Use 'filter[meta_query]' syntax and include 'type=DATE'
                var metaQuery = new List<string>
                {
                    $"filter[meta_query][0][key]=mphb_check_in_date",
                    $"filter[meta_query][0][value]={formattedStartDate}",
                    $"filter[meta_query][0][compare]=>=",
                    $"filter[meta_query][0][type]=DATE",

                    $"filter[meta_query][1][key]=mphb_check_in_date",
                    $"filter[meta_query][1][value]={formattedEndDate}",
                    $"filter[meta_query][1][compare]=<=",
                    $"filter[meta_query][1][type]=DATE"
                };
                var queryParams = new List<string>
                {
                $"page={page}",
                $"per_page={perPage}",
                string.Join("&", metaQuery)
                };

                var requestUrl = $"{_httpClient.BaseAddress}?{string.Join("&", queryParams)}";

                var byteArray = Encoding.ASCII.GetBytes($"{_username}:{_encryptedPassword}");
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JToken.Parse(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching booking data from API");
                throw;
            }
        }


        // Handle pagination
        public async Task<List<JToken>> GetAllBookingsAsync(DateTime? afterDate = null, int perPage = 100)
        {
            var allBookings = new List<JToken>();
            int page = 1;

            while (true)
            {
                var bookingData = await GetBookingDataAsync(afterDate, page, perPage);

                if (bookingData == null || !bookingData.HasValues)
                    break; // Stop if no data is returned

                allBookings.AddRange(bookingData);

                // If less than perPage results are returned, we assume it's the last page
                if (bookingData.Count() < perPage)
                    break;

                page++;
            }

            return allBookings;
        }
    }
}
