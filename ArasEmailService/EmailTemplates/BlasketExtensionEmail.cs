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
            if (suiteNumber != 0)
            {
                var map = new System.Collections.Generic.Dictionary<int, string>
                {
                    {7,  "0726"},
                    {8,  "0826"},
                    {9,  "0926"},
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
            <p>To get here, please enter through the <span style='color:purple'>PURPLE</span> front door with keypad code <strong>0669</strong>.</p>
            {collectionHtml}
            <p>Welcome to the outdoor extension. Please enter via the courtyard / back entrance. The courtyard/back keypads use code <strong>0669</strong>. You will find {(suiteNumber != 0 ? $"Suite {suiteNumber}" : "your suite")} from the courtyard; the lockbox code for this suite is <strong>{lockboxCode}</strong>.</p>

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

            return $@"
            <h3>{mainHeader}</h3>
            <p>We look forward to welcoming you to our brand new extension in the Blasket Suite.</p>

            <p>To get here, please enter through the <span style='color:purple'>PURPLE</span> front door with keypad code <strong>0669</strong>. You then enter the door to the left under the stairs, continue out the back door and you will see our new extension. Continue along the outer Georgian coloured doors to the Oak main entrance of the new section. The keypad code here is <strong>0669</strong> also (same for back door entrance too). You will find {(suiteNumber != 0 ? $"Suite {suiteNumber}" : "your suite")} up the staircase all the way to the left. The lockbox code for this suite is <strong>{lockboxCode}</strong>.</p>

            {mainCollectionHtml}
            <p>Please feel free to use the complimentary communal kitchen area downstairs if you wish also. Enjoy 😊</p>
    ";
        }
    }
}
