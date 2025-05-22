using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace ArasEmailService.Services.Interfaces
{
    public interface IBookingApiClient
    {
        /// <summary>
        /// Fetches booking data from the external API asynchronously with dynamic filtering and pagination.
        /// </summary>
        /// <param name="afterDate">The date to filter bookings after (optional).</param>
        /// <param name="page">The page number for pagination.</param>
        /// <param name="perPage">The number of bookings per page.</param>
        /// <returns>A Task containing a JToken with booking data, or null if the operation fails.</returns>
        Task<JToken> GetBookingDataAsync(DateTime? afterDate = null, int page = 1, int perPage = 100);
    }
}
