using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArasEmailService.EmailTemplates
{
    public class Apt2WBYeatsEmail
    {
        public static string Instructions(JToken booking)
        {
            return $@"
            <h4>Apt. 2: W.B. Yeats</h4>
            <p>Please enter through the <span style='color:purple'>PURPLE</span> front door with keypad code <strong>0669</strong>. 
            Your Apt. #2 is up the stairs on the 1st floor. The key lockbox code is <strong>0221</strong>. Please scramble the code after retrieving keys, and after leaving. 
            Thank you!</p>
            
            <h3>Amenities</h3>
            <ul>
                <li><strong>Kitchen Items:</strong> Dishes, glasses, and cups are in the upper cabinets. Silverware is in the drawer. Cooking utensils are in the bottom cabinet. 
                Feel free to use any of these.</li>
                <li><strong>Dining Table:</strong> Please use placemats provided and enjoy your meal. When finished, use the dish under the sink for washing up.</li>
                <li><strong>Heating:</strong> There is central heating in the apartment.</li>
                <li><strong>Hot Water:</strong> Let the hot water tap run for a while until it’s hot.</li>
                <li><strong>Toilet:</strong> Please, nothing but toilet paper should go in the toilet. There’s a small trash bin for everything else.</li>
                <li><strong>Electric Hob Instructions:</strong>
                    <ol>
                        <li>To turn on or off the hob: Hold down the switch on the second row, far right, for 3 seconds.</li>
                        <li>Choose the ring you wish to use: Press either switch far left.</li>
                        <li>Adjust heat using the plus and minus signs in the center.</li>
                    </ol>
                </li>
            </ul>
    ";
        }
    }
}
