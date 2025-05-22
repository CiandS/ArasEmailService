using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArasEmailService.Models
{
    public class ArrivalGuide
    {
        public int Id { get; set; }
        public List<HtmlString> Instructions { get; set; }
        public string CustomerName { get; set; }
        public string DirectionsLink { get; set; }
        public string ParkingLink { get; set; }
        public string ReviewLink { get; set; }
    }
}
  