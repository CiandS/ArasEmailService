using Newtonsoft.Json.Linq;
using System;
using System.Text.RegularExpressions;

namespace ArasEmailService.EmailTemplates
{
    public class BlasketExtensionEmail
    {
        public static string Instructions(JToken booking)
        {
            // Determine suite and courtyard access based on accommodation id mapping (preferred) or fallback to text parsing
            int suiteNumber = 0;
            bool isCourtyard = false;
            string collectionName = null;

            var accommodationMap = new System.Collections.Generic.Dictionary<int, (int suite, bool courtyard, string collection)>
            {
                { 12571, (7, true, "An Clós (The Courtyard)") },   // Doras Dearg (Suite 7) - courtyard
                { 12574, (8, true, "An Clós (The Courtyard)") },   // Doras Gorm (Suite 8) - courtyard
                { 12573, (9, true, "An Clós (The Courtyard)") },   // Doras Bánbhuí (Suite 9) - courtyard
                { 12569, (10, false, "The Peninsula Collection") }, // Suite 10
                { 12568, (11, false, "The Peninsula Collection") }, // Suite 11
                { 12570, (12, false, "The Peninsula Collection") }, // Suite 12
                { 12567, (13, false, "The Blasket Suite") },       // Blasket Suite (13)
                { 12564, (14, false, "The Brandon Heights") },     // Suite 14
                { 12565, (15, false, "The Brandon Heights") },     // Suite 15
                { 12566, (16, false, "The Brandon Heights") },     // Suite 16
            };

            try
            {
                var reserved = booking["reserved_accommodations"];
                if (reserved != null)
                {
                    foreach (var ra in reserved)
                    {
                        // try id-based detection first
                        var acc = ra["accommodation"];
                        if (acc != null && int.TryParse(acc.ToString(), out var accNum))
                        {
                        if (accommodationMap.TryGetValue(accNum, out var info))
                        {
                            suiteNumber = info.suite;
                            isCourtyard = info.courtyard;
                            // capture collection name if available
                            if (!string.IsNullOrWhiteSpace(info.collection))
                                collectionName = info.collection;
                            break;
                        }
                        }

                        // fallback: try to parse suite number from name/title
                        var fields = new[] { "accommodation_name", "accommodation_title", "title", "name" };
                        foreach (var f in fields)
                        {
                            var text = ra[f]?.ToString();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                var m = Regex.Match(text, "(\\d{1,2})");
                                if (m.Success && int.TryParse(m.Value, out var n))
                                {
                                    suiteNumber = n;
                                    break;
                                }

                                var t = text.ToLowerInvariant();
                                if (t.Contains("courtyard") || t.Contains("outdoor") || t.Contains("outside") || t.Contains("back"))
                                {
                                    isCourtyard = true;
                                }
                            }
                        }

                        if (suiteNumber != 0 || isCourtyard) break;
                    }
                }
            }
            catch
            {
                // ignore and fallback to defaults
            }

            // Map lockbox codes for new extension suites (7..16). Update if codes differ.
            string lockboxCode = "1326"; // default
            string frontDoorCode = "0669";
            if (suiteNumber != 0)
            {
                var map = new System.Collections.Generic.Dictionary<int, string>
                {
                    {7,  "0669"},
                    {8,  "0669"},
                    {9,  "0669"},
                    {10, "1026"},
                    {11, "1126"},
                    {12, "1226"},
                    {13, "1326"},
                    {14, "1426"},
                    {15, "1526"},
                    {16, "1626"}
                };

                if (map.ContainsKey(suiteNumber))
                    lockboxCode = map[suiteNumber];
                else
                    lockboxCode = suiteNumber.ToString() + "26";
            }

            if (isCourtyard)
            {
                var header = !string.IsNullOrWhiteSpace(collectionName)
                    ? $"{collectionName} — Blasket Extension (Courtyard Access)"
                    : "Blasket Suite — Extension (Courtyard Access)";
                if (suiteNumber != 0)
                    header += $" — Suite {suiteNumber}";

                var collectionHtml = !string.IsNullOrWhiteSpace(collectionName)
                    ? $"<p><em>Part of the {collectionName} collection.</em></p>"
                    : string.Empty;

                return $@"
            <h3>{header}</h3>
            {collectionHtml}

            <h4>On Arrival</h4>
            <ol>
                <li>Enter via the <span style='color:purple'><strong>PURPLE</strong></span> front door or courtyard/back entrance.</li>
                <li>Use keypad code <strong>{frontDoorCode}</strong> for the door and lockbox code <strong>{lockboxCode}</strong> to retrieve your keys.</li>
                <li>Your suite will be located in the courtyard {(suiteNumber != 0 ? $"as Suite {suiteNumber}" : "") }.</li>
                <li>Please scramble the lockbox code after retrieving your keys and again when leaving.</li>
            </ol>

            <p>Please feel free to use the complimentary communal kitchen area downstairs if you wish also. Enjoy 😊</p>
    ";
            }

            var mainHeader = !string.IsNullOrWhiteSpace(collectionName)
                ? $"{collectionName} — Blasket Extension"
                : "Blasket Suite — Extension";
            if (suiteNumber != 0)
                mainHeader += $" — Suite {suiteNumber}";

            var mainCollectionHtml = !string.IsNullOrWhiteSpace(collectionName)
                ? $"<p><em>Part of the {collectionName} collection.</em></p>"
                : string.Empty;

            // Only suites 13..16 are upstairs
            var upstairsSuites = new System.Collections.Generic.HashSet<int> { 13, 14, 15, 16 };
            var suiteLabel = suiteNumber != 0 ? $"Suite {suiteNumber}" : "your suite";
            var isUpstairs = suiteNumber != 0 && upstairsSuites.Contains(suiteNumber);
            var locationText = isUpstairs ? "up the staircase." : "on the ground floor.";

            return $@"
            <h3>{mainHeader}</h3>
            <p>We look forward to welcoming you to our brand new extension in the Blasket Suite.</p>

            <h4>On Arrival</h4>
            <ol>
                <li>Enter through the <span style='color:purple'><strong>PURPLE</strong></span> front door.</li>
                <li>Use keypad code <strong>{frontDoorCode}</strong> for the front door and lockbox code <strong>{lockboxCode}</strong> to retrieve your keys.</li>
                <li>You will find {suiteLabel} {locationText}</li>
                <li>Please scramble the lockbox code after retrieving your keys and again when leaving.</li>
            </ol>

            {mainCollectionHtml}
            <p>Please feel free to use the complimentary communal kitchen area downstairs if you wish also. Enjoy 😊</p>
    ";
        }
    }
}
