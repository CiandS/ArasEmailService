using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArasEmailService.Services.Interfaces
{
    public interface IBookingStateService
    {
        Task ProcessBookingStatesAsync(IEnumerable<JToken> bookings);
    }
}
