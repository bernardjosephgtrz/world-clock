using System;
using System.Globalization;

namespace WorldClock
{
    public static class TimeFormat
    {
        public static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

        /// <summary>"+08:00", "-03:00", "+05:30"</summary>
        public static string Offset(TimeSpan off)
        {
            var a = off.Duration();
            return (off < TimeSpan.Zero ? "-" : "+") + ((int)a.TotalHours).ToString("00") + ":" + a.Minutes.ToString("00");
        }

        /// <summary>e.g. "7h behind · Yesterday", "1h ahead · Today", "5h 30m behind · Today".
        /// When planning a time, the day reads "Same day" / "Next day" / "Previous day" instead.</summary>
        public static string Relative(TimeSpan diff, DateTime zoneDate, DateTime homeDate, bool planned = false)
        {
            string rel;
            if (diff == TimeSpan.Zero)
            {
                rel = "Same time as you";
            }
            else
            {
                var a = diff.Duration();
                rel = ((int)a.TotalHours) + "h" + (a.Minutes != 0 ? " " + a.Minutes + "m" : "") +
                      (diff > TimeSpan.Zero ? " ahead" : " behind");
            }

            string day = zoneDate.Date == homeDate.Date ? (planned ? "Same day" : "Today")
                       : zoneDate.Date > homeDate.Date ? (planned ? "Next day" : "Tomorrow")
                       : (planned ? "Previous day" : "Yesterday");
            return rel + " \u00B7 " + day;
        }

        /// <summary>Compact form for the one-line view: "-6h", "+1h", "-2h30m", "" when same time.</summary>
        public static string Short(TimeZoneInfo zone, TimeZoneInfo home, DateTime utcNow)
        {
            var diff = zone.GetUtcOffset(utcNow) - home.GetUtcOffset(utcNow);
            if (diff == TimeSpan.Zero) return "";
            var a = diff.Duration();
            return (diff > TimeSpan.Zero ? "+" : "\u2212") + ((int)a.TotalHours) + "h" + (a.Minutes != 0 ? a.Minutes.ToString("00") : "");
        }

        /// <summary>Description for a zone relative to home at a given UTC instant.</summary>
        public static string Describe(TimeZoneInfo zone, TimeZoneInfo home, DateTime utcNow, bool planned = false)
        {
            var zoneLocal = TimeZoneInfo.ConvertTimeFromUtc(utcNow, zone);
            var homeLocal = TimeZoneInfo.ConvertTimeFromUtc(utcNow, home);
            return Relative(zone.GetUtcOffset(utcNow) - home.GetUtcOffset(utcNow), zoneLocal, homeLocal, planned);
        }

        // ---- planning a time ----------------------------------------------------

        /// <summary>Parses a typed clock time: "3pm", "3 PM", "3:30pm", "3.30 pm", "15:00", "15.00", "1500", "9a",
        /// "noon", "midnight". Without am/pm the hour is 24-hour ("3" = 03:00).</summary>
        public static bool TryParseClock(string text, out int hour, out int minute)
        {
            hour = minute = 0;
            if (text == null) return false;
            string s = text.Trim().ToLowerInvariant().Replace(" ", "").Replace(".", "");
            if (s == "noon") { hour = 12; return true; }
            if (s == "midnight") return true;

            int ap = 0;   // 1 = am, 2 = pm
            if (s.EndsWith("am")) { ap = 1; s = s.Substring(0, s.Length - 2); }
            else if (s.EndsWith("pm")) { ap = 2; s = s.Substring(0, s.Length - 2); }
            else if (s.EndsWith("a")) { ap = 1; s = s.Substring(0, s.Length - 1); }
            else if (s.EndsWith("p")) { ap = 2; s = s.Substring(0, s.Length - 1); }

            string hs, ms;
            int colon = s.IndexOf(':');
            if (colon >= 0) { hs = s.Substring(0, colon); ms = s.Substring(colon + 1); }
            else if (s.Length >= 3) { hs = s.Substring(0, s.Length - 2); ms = s.Substring(s.Length - 2); }
            else { hs = s; ms = "0"; }
            if (!Digits(hs) || !Digits(ms)) return false;

            hour = int.Parse(hs);
            minute = int.Parse(ms);
            if (minute > 59) return false;
            if (ap != 0)
            {
                if (hour < 1 || hour > 12) return false;
                hour = hour % 12 + (ap == 2 ? 12 : 0);
            }
            return hour <= 23;
        }

        static bool Digits(string s)
        {
            if (s.Length == 0 || s.Length > 2) return false;
            foreach (char c in s) if (c < '0' || c > '9') return false;
            return true;
        }

        /// <summary>UTC instant for a wall-clock time in a zone. A time skipped by a DST jump moves forward
        /// by the jump (02:30 on spring-forward day becomes 03:30).</summary>
        public static DateTime ZoneToUtc(DateTime wall, TimeZoneInfo zone)
        {
            wall = DateTime.SpecifyKind(wall, DateTimeKind.Unspecified);
            if (zone.IsInvalidTime(wall))
            {
                // Size of the jump = offset just after it minus offset just before it.
                var jump = zone.GetUtcOffset(wall.AddHours(3)) - zone.GetUtcOffset(wall.AddHours(-3));
                wall = wall.Add(jump > TimeSpan.Zero ? jump : TimeSpan.FromHours(1));
            }
            return TimeZoneInfo.ConvertTimeToUtc(wall, zone);
        }

        /// <summary>Moves to the next/previous multiple of <paramref name="minutes"/> on the zone's wall clock:
        /// 10:07 +15 gives 10:15, 10:07 -15 gives 10:00, 10:15 +15 gives 10:30.</summary>
        public static DateTime Step(DateTime utc, TimeZoneInfo zone, int minutes, int dir)
        {
            long t = TimeZoneInfo.ConvertTimeFromUtc(utc, zone).TimeOfDay.Ticks;
            long s = minutes * TimeSpan.TicksPerMinute;
            long target = dir > 0 ? (t / s + 1) * s : ((t + s - 1) / s - 1) * s;
            return utc.AddTicks(target - t);
        }

        /// <summary>Same wall-clock minute in the zone, one day later/earlier (stays correct across DST).</summary>
        public static DateTime StepDay(DateTime utc, TimeZoneInfo zone, int dir)
        {
            var w = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
            w = new DateTime(w.Year, w.Month, w.Day, w.Hour, w.Minute, 0);
            return ZoneToUtc(w.AddDays(dir), zone);
        }
    }
}
