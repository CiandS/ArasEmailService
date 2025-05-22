using System;
using System.Threading.Tasks;

namespace ArasEmailService.Services.Interfaces
{
    public interface ILogRepository
    {
        /// <summary>
        /// Interface for logging email operations related to bookings.
        /// </summary>
        Task<bool> HasEmailBeenSentAsync(int bookingId, DateTime startDate);
        Task RecordEmailSentAsync(int bookingId, string emailType);
    }
}
