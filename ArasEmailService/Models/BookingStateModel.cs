using System;

namespace ArasEmailService.Models
{
    public class BookingStateModel
    {
        public string UID { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int AccommodationId { get; set; }
        public string GuestName { get; set; }
        public string SnapshotHash { get; set; }
    }
}
