using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WorldClock
{
    public class ZoneEntry
    {
        public string City;
        public string Country;   // null for raw Windows time zones
        public string TzId;
        public TimeZoneInfo Tz;
        public string SearchKey;

        public string Label
        {
            get { return string.IsNullOrEmpty(Country) ? City : City + ", " + Country; }
        }
    }

    /// <summary>
    /// Offline time zone catalogue. Uses the time zone database built into Windows,
    /// so daylight saving rules are applied automatically with no internet access.
    /// </summary>
    public static class Zones
    {
        /// <summary>Windows maps Asia/Manila (UTC+8, no DST) to this zone.</summary>
        public const string HomeTzId = "Singapore Standard Time";

        // City|Country|Windows time zone id
        static readonly string[] Cities =
        {
            "Hamburg|Germany|W. Europe Standard Time",
            "Berlin|Germany|W. Europe Standard Time",
            "Bremen|Germany|W. Europe Standard Time",
            "Frankfurt|Germany|W. Europe Standard Time",
            "Munich|Germany|W. Europe Standard Time",
            "Düsseldorf|Germany|W. Europe Standard Time",
            "Schindellegi|Switzerland|W. Europe Standard Time",
            "Zurich|Switzerland|W. Europe Standard Time",
            "Basel|Switzerland|W. Europe Standard Time",
            "Amsterdam|Netherlands|W. Europe Standard Time",
            "Rotterdam|Netherlands|W. Europe Standard Time",
            "Luxembourg|Luxembourg|W. Europe Standard Time",
            "Vienna|Austria|W. Europe Standard Time",
            "Rome|Italy|W. Europe Standard Time",
            "Milan|Italy|W. Europe Standard Time",
            "Stockholm|Sweden|W. Europe Standard Time",
            "Oslo|Norway|W. Europe Standard Time",
            "Copenhagen|Denmark|Romance Standard Time",
            "Paris|France|Romance Standard Time",
            "Brussels|Belgium|Romance Standard Time",
            "Antwerp|Belgium|Romance Standard Time",
            "Madrid|Spain|Romance Standard Time",
            "Barcelona|Spain|Romance Standard Time",
            "Warsaw|Poland|Central European Standard Time",
            "Zagreb|Croatia|Central European Standard Time",
            "Prague|Czechia|Central Europe Standard Time",
            "Budapest|Hungary|Central Europe Standard Time",
            "Bratislava|Slovakia|Central Europe Standard Time",
            "Belgrade|Serbia|Central Europe Standard Time",
            "London|United Kingdom|GMT Standard Time",
            "Manchester|United Kingdom|GMT Standard Time",
            "Dublin|Ireland|GMT Standard Time",
            "Lisbon|Portugal|GMT Standard Time",
            "Tallinn|Estonia|FLE Standard Time",
            "Riga|Latvia|FLE Standard Time",
            "Vilnius|Lithuania|FLE Standard Time",
            "Helsinki|Finland|FLE Standard Time",
            "Kyiv|Ukraine|FLE Standard Time",
            "Sofia|Bulgaria|FLE Standard Time",
            "Athens|Greece|GTB Standard Time",
            "Bucharest|Romania|GTB Standard Time",
            "Istanbul|Turkey|Turkey Standard Time",
            "Moscow|Russia|Russian Standard Time",
            "Tel Aviv|Israel|Israel Standard Time",
            "Cairo|Egypt|Egypt Standard Time",
            "Casablanca|Morocco|Morocco Standard Time",
            "Lagos|Nigeria|W. Central Africa Standard Time",
            "Johannesburg|South Africa|South Africa Standard Time",
            "Nairobi|Kenya|E. Africa Standard Time",
            "Dubai|United Arab Emirates|Arabian Standard Time",
            "Riyadh|Saudi Arabia|Arab Standard Time",
            "Doha|Qatar|Arab Standard Time",
            "Karachi|Pakistan|Pakistan Standard Time",
            "Mumbai|India|India Standard Time",
            "New Delhi|India|India Standard Time",
            "Bengaluru|India|India Standard Time",
            "Chennai|India|India Standard Time",
            "Colombo|Sri Lanka|Sri Lanka Standard Time",
            "Dhaka|Bangladesh|Bangladesh Standard Time",
            "Bangkok|Thailand|SE Asia Standard Time",
            "Ho Chi Minh City|Vietnam|SE Asia Standard Time",
            "Hanoi|Vietnam|SE Asia Standard Time",
            "Jakarta|Indonesia|SE Asia Standard Time",
            "Kuala Lumpur|Malaysia|Singapore Standard Time",
            "Singapore|Singapore|Singapore Standard Time",
            "Manila|Philippines|Singapore Standard Time",
            "Cebu|Philippines|Singapore Standard Time",
            "Davao|Philippines|Singapore Standard Time",
            "Hong Kong|Hong Kong|China Standard Time",
            "Shanghai|China|China Standard Time",
            "Beijing|China|China Standard Time",
            "Shenzhen|China|China Standard Time",
            "Taipei|Taiwan|Taipei Standard Time",
            "Seoul|South Korea|Korea Standard Time",
            "Tokyo|Japan|Tokyo Standard Time",
            "Perth|Australia|W. Australia Standard Time",
            "Adelaide|Australia|Cen. Australia Standard Time",
            "Brisbane|Australia|E. Australia Standard Time",
            "Sydney|Australia|AUS Eastern Standard Time",
            "Melbourne|Australia|AUS Eastern Standard Time",
            "Auckland|New Zealand|New Zealand Standard Time",
            "Honolulu|United States|Hawaiian Standard Time",
            "Anchorage|United States|Alaskan Standard Time",
            "Los Angeles|United States|Pacific Standard Time",
            "San Francisco|United States|Pacific Standard Time",
            "Seattle|United States|Pacific Standard Time",
            "Vancouver|Canada|Pacific Standard Time",
            "Phoenix|United States|US Mountain Standard Time",
            "Denver|United States|Mountain Standard Time",
            "Chicago|United States|Central Standard Time",
            "Dallas|United States|Central Standard Time",
            "Houston|United States|Central Standard Time",
            "Mexico City|Mexico|Central Standard Time (Mexico)",
            "New York|United States|Eastern Standard Time",
            "Atlanta|United States|Eastern Standard Time",
            "Miami|United States|Eastern Standard Time",
            "Toronto|Canada|Eastern Standard Time",
            "Montreal|Canada|Eastern Standard Time",
            "Panama City|Panama|SA Pacific Standard Time",
            "Bogotá|Colombia|SA Pacific Standard Time",
            "Lima|Peru|SA Pacific Standard Time",
            "Santiago|Chile|Pacific SA Standard Time",
            "Buenos Aires|Argentina|Argentina Standard Time",
            "São Paulo|Brazil|E. South America Standard Time",
            "Rio de Janeiro|Brazil|E. South America Standard Time",
            "UTC|Coordinated Universal Time|UTC",
        };

        static List<ZoneEntry> cache;

        /// <summary>Named cities first (A-Z), then every Windows time zone (by offset).</summary>
        public static List<ZoneEntry> All()
        {
            if (cache != null) return cache;

            var sys = new Dictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var tz in TimeZoneInfo.GetSystemTimeZones()) sys[tz.Id] = tz;

            var cities = new List<ZoneEntry>();
            foreach (var row in Cities)
            {
                var p = row.Split('|');
                TimeZoneInfo tz;
                if (sys.TryGetValue(p[2], out tz)) cities.Add(Make(p[0], p[1], tz));
            }
            cities.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.CurrentCultureIgnoreCase));

            var raw = new List<TimeZoneInfo>(sys.Values);
            raw.Sort((a, b) =>
            {
                int c = a.BaseUtcOffset.CompareTo(b.BaseUtcOffset);
                return c != 0 ? c : string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
            });

            var list = new List<ZoneEntry>(cities);
            foreach (var tz in raw) list.Add(Make(StripOffsetPrefix(tz.DisplayName), null, tz));
            cache = list;
            return list;
        }

        /// <summary>The fixed first clock: Cebu, Philippines.</summary>
        public static ZoneEntry Home()
        {
            TimeZoneInfo tz;
            try { tz = TimeZoneInfo.FindSystemTimeZoneById(HomeTzId); }
            catch (Exception)
            {
                // Philippines has been UTC+8 with no DST since 1978, so a fixed zone is a safe fallback.
                tz = TimeZoneInfo.CreateCustomTimeZone("Philippine Standard Time", TimeSpan.FromHours(8),
                    "(UTC+08:00) Philippines", "Philippine Standard Time");
            }
            return Make("Cebu", "Philippines", tz);
        }

        public static ZoneEntry Find(SavedZone saved)
        {
            foreach (var z in All())
                if (z.TzId == saved.TzId && z.City == saved.City) return z;
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(saved.TzId);
                return Make(saved.City, string.IsNullOrEmpty(saved.Country) ? null : saved.Country, tz);
            }
            catch (Exception) { return null; }
        }

        public static ZoneEntry FindCity(string city)
        {
            foreach (var z in All())
                if (z.Country != null && string.Equals(z.City, city, StringComparison.OrdinalIgnoreCase)) return z;
            return null;
        }

        /// <summary>True when every whitespace-separated term of the query appears in the entry.</summary>
        public static bool Matches(ZoneEntry z, string query)
        {
            var q = Normalize(query ?? "").Trim();
            if (q.Length == 0) return true;
            foreach (var term in q.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                if (z.SearchKey.IndexOf(term, StringComparison.Ordinal) < 0) return false;
            return true;
        }

        static ZoneEntry Make(string city, string country, TimeZoneInfo tz)
        {
            var z = new ZoneEntry { City = city, Country = country, TzId = tz.Id, Tz = tz };
            var off = tz.BaseUtcOffset;
            var a = off.Duration();
            string shortOff = (off < TimeSpan.Zero ? "-" : "+") + a.Hours + (a.Minutes != 0 ? ":" + a.Minutes.ToString("00") : "");
            z.SearchKey = Normalize(z.Label + " " + tz.Id + " " + tz.DisplayName + " " + tz.StandardName + " " +
                                    tz.DaylightName + " utc" + shortOff + " gmt" + shortOff);
            return z;
        }

        static string StripOffsetPrefix(string displayName)
        {
            int i = displayName.IndexOf(") ", StringComparison.Ordinal);
            return displayName.StartsWith("(") && i > 0 ? displayName.Substring(i + 2) : displayName;
        }

        /// <summary>Lower-case and strip accents so "sao paulo" finds "São Paulo".</summary>
        public static string Normalize(string s)
        {
            var d = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            foreach (char ch in d)
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    sb.Append(char.ToLowerInvariant(ch));
            return sb.ToString();
        }
    }
}
