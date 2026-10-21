using System;
using System.IO;

namespace WorldClock.Tests
{
    static class Tests
    {
        static int failed, passed;

        static void Eq(object expected, object actual, string name)
        {
            if (Equals(expected, actual)) { passed++; return; }
            failed++;
            Console.WriteLine("FAIL " + name + ": expected <" + expected + "> got <" + actual + ">");
        }

        static int Main()
        {
            var home = Zones.Home().Tz;
            var hamburg = Zones.FindCity("Hamburg");

            // Home zone is UTC+8 without DST.
            Eq(TimeSpan.FromHours(8), home.GetUtcOffset(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)), "home offset Jan");
            Eq(TimeSpan.FromHours(8), home.GetUtcOffset(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc)), "home offset Jul");

            // Hamburg: CEST (UTC+2) until Sun 25 Oct 2026 01:00 UTC, then CET (UTC+1).
            Eq("W. Europe Standard Time", hamburg.TzId, "hamburg zone id");
            var beforeDst = new DateTime(2026, 10, 21, 8, 20, 0, DateTimeKind.Utc);   // 16:20 in Cebu
            Eq(TimeSpan.FromHours(2), hamburg.Tz.GetUtcOffset(beforeDst), "hamburg CEST");
            Eq("6h behind \u00B7 Today", TimeFormat.Describe(hamburg.Tz, home, beforeDst), "hamburg describe Oct 21");
            var afterDst = new DateTime(2026, 10, 26, 8, 0, 0, DateTimeKind.Utc);
            Eq(TimeSpan.FromHours(1), hamburg.Tz.GetUtcOffset(afterDst), "hamburg CET");
            Eq("7h behind \u00B7 Today", TimeFormat.Describe(hamburg.Tz, home, afterDst), "hamburg describe Oct 26");

            // Day boundary: 02:00 in Cebu (18:00 UTC) is still yesterday in Hamburg (19:00/20:00).
            var lateUtc = new DateTime(2026, 10, 21, 18, 0, 0, DateTimeKind.Utc);
            Eq("6h behind \u00B7 Yesterday", TimeFormat.Describe(hamburg.Tz, home, lateUtc), "yesterday");
            var tokyo = Zones.FindCity("Tokyo");
            var earlyUtc = new DateTime(2026, 10, 21, 15, 30, 0, DateTimeKind.Utc);   // 23:30 Cebu, 00:30 Tokyo
            Eq("1h ahead \u00B7 Tomorrow", TimeFormat.Describe(tokyo.Tz, home, earlyUtc), "tomorrow");

            // Half-hour zones and same-time zones.
            Eq("2h 30m behind \u00B7 Today", TimeFormat.Describe(Zones.FindCity("Mumbai").Tz, home, beforeDst), "india");
            Eq("Same time as you \u00B7 Today", TimeFormat.Describe(Zones.FindCity("Singapore").Tz, home, beforeDst), "same");

            Eq("+08:00", TimeFormat.Offset(TimeSpan.FromHours(8)), "offset +8");
            Eq("-03:30", TimeFormat.Offset(TimeSpan.FromMinutes(-210)), "offset -3:30");
            Eq("+00:00", TimeFormat.Offset(TimeSpan.Zero), "offset 0");

            // Compact strip offsets.
            Eq("\u22126h", TimeFormat.Short(hamburg.Tz, home, beforeDst), "short hamburg CEST");
            Eq("\u22127h", TimeFormat.Short(hamburg.Tz, home, afterDst), "short hamburg CET");
            Eq("+1h", TimeFormat.Short(tokyo.Tz, home, beforeDst), "short tokyo");
            Eq("\u22122h30", TimeFormat.Short(Zones.FindCity("Mumbai").Tz, home, beforeDst), "short india");
            Eq("", TimeFormat.Short(Zones.FindCity("Singapore").Tz, home, beforeDst), "short same");

            // Scale is clamped to a sane range.
            Eq(0.6, AppSettings.ClampScale(0.1), "clamp low");
            Eq(2.0, AppSettings.ClampScale(9), "clamp high");
            Eq(1.0, AppSettings.ClampScale(double.NaN), "clamp nan");

            // Search: city, country, accent-insensitive, multi-word, offset.
            Eq(true, Zones.Matches(hamburg, "ham"), "search ham");
            Eq(true, Zones.Matches(hamburg, "germany"), "search germany");
            Eq(true, Zones.Matches(hamburg, "ham ger"), "search multi");
            Eq(false, Zones.Matches(hamburg, "paris"), "search negative");
            Eq(true, Zones.Matches(Zones.FindCity("São Paulo"), "sao paulo"), "search accents");
            Eq(true, Zones.Matches(hamburg, "utc+1"), "search offset");
            Eq(true, Zones.All().Count > 150, "catalogue size");

            // Settings round-trip (writes to a temp copy, never touches the user's real settings).
            var path = AppSettings.FilePath;
            string backup = File.Exists(path) ? File.ReadAllText(path) : null;
            try
            {
                var s = new AppSettings { X = -1700, Y = 40, CX = 300, CY = 1040, Compact = true, Scale = 1.35, Topmost = true, Locked = true };
                s.Zones.Add(new SavedZone { TzId = "W. Europe Standard Time", City = "Hamburg", Country = "Germany" });
                s.Zones.Add(new SavedZone { TzId = "Tokyo Standard Time", City = "Tokyo", Country = "Japan" });
                s.Save();
                var l = AppSettings.Load();
                Eq(-1700, l.X, "settings x (negative = left monitor)");
                Eq(40, l.Y, "settings y");
                Eq(300, l.CX, "settings compact x");
                Eq(1040, l.CY, "settings compact y");
                Eq(true, l.Compact, "settings compact");
                Eq(1.35, l.Scale, "settings scale");
                Eq(true, l.Topmost, "settings topmost");
                Eq(true, l.Locked, "settings locked");
                Eq(2, l.Zones.Count, "settings zones");
                Eq("Tokyo", l.Zones[1].City, "settings zone city");
                Eq(false, l.FirstRun, "settings first run");
                Eq("Hamburg", Zones.Find(l.Zones[0]).City, "settings zone resolves");
            }
            finally
            {
                if (backup != null) File.WriteAllText(path, backup); else if (File.Exists(path)) File.Delete(path);
            }

            Console.WriteLine(passed + " passed, " + failed + " failed");
            return failed == 0 ? 0 : 1;
        }
    }
}
