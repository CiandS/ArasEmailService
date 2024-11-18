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
        public static string GenerateEmailBody(JToken booking)
        {
            string customerName = $"{booking["customer"]["first_name"]}";
            string directionsLink = "https://maps.app.goo.gl/foZGz3oMBNLWaTNy6";
            string parkingLink = "https://maps.app.goo.gl/bVaQ81k5PjvWeten6";
            string reviewLink = "https://g.page/r/CbQcbuEAJu6REBM/review";

            return $@"
        <html>
        <body>
            <p>Dear {customerName},</p>
            <p><strong>Fáilte (Welcome)!</strong> If you need anything before, during, or after your stay, please feel free to reach me at <a href='mailto:info@arasdestaic.com'>info@arasdestaic.com</a>. 
            You can also contact us through the website live chat if you prefer.</p>
            <p>We look forward to having you with us.</p>

            <h3>Stay Instructions for Áras de Staic</h3>

            <p><strong>Google maps pin to us:</strong> <a href='{directionsLink}'>Find us on Google Maps</a></p>
            <p><strong>Address:</strong> Green St, Dingle, Co. Kerry, V92XDK6, Ireland</p>

            <p><strong>Check-in Time:</strong> 3pm</p>

            <h4>Apt. 2: Oscar Wilde</h4>
            <p>Please enter through the <span style='color:purple'>PURPLE</span> front door with keypad code <strong>0669</strong>. 
            Your Apt. #2 is up the stairs on the 1st floor. The key lockbox code is <strong>0221</strong>. Please scramble the code after retrieving keys, and after leaving. 
            Thank you!</p>
            
            <p><strong>Check-out Time:</strong> 11am</p>
            <p><em>Go raibh maith agat (Thank you)!</em></p>

            <h3>Wifi Details</h3>
            <p><strong>Network:</strong> ArasdeStaic</p>
            <p><strong>Password:</strong> arasdingle21</p>
            <p>Enjoy your stay!</p>

            <h3>Parking</h3>
            <p>While there is free street parking, please respect local residents and use the free parking lot across the street, just beside the church. 
            Here is a pin to the carpark: <a href='{parkingLink}'>Parking Location</a>.</p>
            <p>There are also paid parking options around town.</p>

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

            <p>We would really appreciate your reviews on Google if you have time: <a href='{reviewLink}'>Leave a review on Google</a></p>

            <p>Kind regards,</p>
            <p>Cian,<br>Áras de Staic</p>
        </body>
        </html>
    ";
        }
    }
}
