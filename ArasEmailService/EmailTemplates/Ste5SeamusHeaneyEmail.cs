using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArasEmailService.EmailTemplates
{
    public class Ste5SeamusHeaneyEmail
    {
        public static string Instructions (JToken booking)
        {
            return $@"
            <h3>Ste. 5: Seamus Heaney</h3>

            <h4>On Arrival</h4>

            <ol>
                <li>Enter through the <span style='color:purple'><strong>PURPLE</strong></span> front door.</li>
                <li>Use keypad code <strong>0669</strong> for the front door and lockbox code <strong>0521</strong> to retrieve your keys.</li>
                <li>Your suite is located <strong>up the stairs on the 1st floor</strong>.</li>
                <li>Please scramble the lockbox code after retrieving your keys and again when leaving.</li>
            </ol>

            <h4>Amenities</h4>
            <ul>
                <li><strong>Kitchen Items:</strong> Dishes, glasses, and cups are in the upper cabinets. Silverware is in the drawer. Cooking utensils are in the bottom cabinet. 
                Feel free to use any of these.</li>
                <li><strong>Dining Table:</strong> Please use placemats provided and enjoy your meal. When finished, use the dish under the sink for washing up.</li>
                <li><strong>Heating:</strong> There is central heating in the apartment. This is on a scheduled timer. Please ensure all valves are fully open. If you feel it is cold and the heating isnt on just reach out and we can boost the system on demand.</li>
                <li><strong>Hot Water:</strong> Let the hot water tap run for a while until it’s hot.</li>
                <li><strong>Toilet:</strong> Please, nothing but toilet paper should go in the toilet. There’s a small trash bin for everything else.</li>
            </ul>
    ";
        }
    }
}
