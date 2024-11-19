using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArasEmailService.Models
{
    internal class ArrivalGuide
    {
        public int Id { get; set; }
        public int KeypadCode { get; set; }
        public int LockboxCode { get; set; }
        public string CustomerName { get; set; }
        public string DirectionsLink { get; set; }
        public string ParkingLink { get; set; }
    }
}
