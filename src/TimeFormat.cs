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

        /// <summary>e.g. "7h behind · Yesterday", "1h ahead · Today", "5h 30m behind · Today".</summary>
        public static string Relative(TimeSpan diff, DateTime zoneDate, DateTime homeDate)
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

            string day = zoneDate.Date == homeDate.Date ? "Today"
                       : zoneDate.Date > homeDate.Date ? "Tomorrow" : "Yesterday";
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
        public static string Describe(TimeZoneInfo zone, TimeZoneInfo home, DateTime utcNow)
        {
            var zoneLocal = TimeZoneInfo.ConvertTimeFromUtc(utcNow, zone);
            var homeLocal = TimeZoneInfo.ConvertTimeFromUtc(utcNow, home);
            return Relative(zone.GetUtcOffset(utcNow) - home.GetUtcOffset(utcNow), zoneLocal, homeLocal);
        }
    }
}
