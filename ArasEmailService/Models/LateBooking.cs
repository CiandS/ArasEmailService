using System;
using System.Collections.Generic;

namespace ArasEmailService.Models
{
    // Minimal model for late-booking emails (not an arrival guide)
    public class LateBooking
    {
        public int Id { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public List<string> Accommodations { get; set; } = new();
    }
}