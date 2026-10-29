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

            // Planning: typed times.
            foreach (var c in new[] {
                new object[] { "3pm", 15, 0 }, new object[] { "3 PM", 15, 0 }, new object[] { "3:30pm", 15, 30 },
                new object[] { "3.30 pm", 15, 30 }, new object[] { "15:00", 15, 0 }, new object[] { "15.00", 15, 0 },
                new object[] { "1500", 15, 0 }, new object[] { "930", 9, 30 }, new object[] { "9a", 9, 0 },
                new object[] { "12am", 0, 0 }, new object[] { "12:15 pm", 12, 15 }, new object[] { "noon", 12, 0 },
                new object[] { "midnight", 0, 0 }, new object[] { " 10:00 PM ", 22, 0 }, new object[] { "0:05", 0, 5 } })
            {
                int h, m;
                bool ok = TimeFormat.TryParseClock((string)c[0], out h, out m);
                Eq(true, ok, "parse ok '" + c[0] + "'");
                Eq((int)c[1] * 60 + (int)c[2], h * 60 + m, "parse '" + c[0] + "'");
            }
            foreach (var bad in new[] { "", "abc", "25:00", "13pm", "0am", "3:75", "12345", "3:", ":30", "1:2:3" })
            {
                int h, m;
                Eq(false, TimeFormat.TryParseClock(bad, out h, out m), "parse rejects '" + bad + "'");
            }

            // The use case: 3 PM in Hamburg on Thu 29 Oct 2026 (CET, after the DST switch) = 10 PM in Cebu.
            var meeting = TimeFormat.ZoneToUtc(new DateTime(2026, 10, 29, 15, 0, 0), hamburg.Tz);
            Eq(new DateTime(2026, 10, 29, 14, 0, 0), meeting, "hamburg 3pm -> utc (CET)");
            Eq(new DateTime(2026, 10, 29, 22, 0, 0), TimeZoneInfo.ConvertTimeFromUtc(meeting, home), "hamburg 3pm -> cebu 10pm");
            Eq("7h behind · Same day", TimeFormat.Describe(hamburg.Tz, home, meeting, true), "planned describe");
            // A week earlier, still CEST: 3 PM Hamburg = 9 PM Cebu.
            var meetingCest = TimeFormat.ZoneToUtc(new DateTime(2026, 10, 21, 15, 0, 0), hamburg.Tz);
            Eq(new DateTime(2026, 10, 21, 21, 0, 0), TimeZoneInfo.ConvertTimeFromUtc(meetingCest, home), "hamburg 3pm -> cebu 9pm (CEST)");
            // Planned day words.
            var late = TimeFormat.ZoneToUtc(new DateTime(2026, 10, 29, 20, 0, 0), hamburg.Tz);   // 03:00 next day in Cebu
            Eq("7h behind · Previous day", TimeFormat.Describe(hamburg.Tz, home, late, true), "planned previous day");
            Eq("1h ahead · Next day", TimeFormat.Describe(tokyo.Tz, home, earlyUtc, true), "planned next day");
            // 02:30 does not exist in Hamburg on 29 Mar 2026 (02:00 -> 03:00): becomes 03:30 CEST = 01:30 UTC.
            Eq(new DateTime(2026, 3, 29, 1, 30, 0), TimeFormat.ZoneToUtc(new DateTime(2026, 3, 29, 2, 30, 0), hamburg.Tz), "dst gap");

            // Scroll steps snap to the zone's wall clock.
            var at1007 = new DateTime(2026, 10, 29, 2, 7, 23, DateTimeKind.Utc);   // 10:07:23 in Cebu
            Func<DateTime, string> cebu = u => TimeZoneInfo.ConvertTimeFromUtc(u, home).ToString("MM-dd HH:mm:ss");
            Eq("10-29 10:15:00", cebu(TimeFormat.Step(at1007, home, 15, 1)), "step +15 snaps up");
            Eq("10-29 10:00:00", cebu(TimeFormat.Step(at1007, home, 15, -1)), "step -15 snaps down");
            Eq("10-29 10:30:00", cebu(TimeFormat.Step(TimeFormat.Step(at1007, home, 15, 1), home, 15, 1)), "step +15 twice");
            Eq("10-29 11:00:00", cebu(TimeFormat.Step(at1007, home, 60, 1)), "step +1h");
            Eq("10-28 23:45:00", cebu(TimeFormat.Step(new DateTime(2026, 10, 28, 16, 0, 0, DateTimeKind.Utc), home, 15, -1)), "step back over midnight");
            Eq("10-30 10:07:00", cebu(TimeFormat.StepDay(at1007, home, 1)), "next day");
            // Day step keeps the wall-clock time across Hamburg's DST switch (Sat 24 -> Sun 25 Oct).
            var sat3pm = TimeFormat.ZoneToUtc(new DateTime(2026, 10, 24, 15, 0, 0), hamburg.Tz);
            Eq(new DateTime(2026, 10, 25, 15, 0, 0), TimeZoneInfo.ConvertTimeFromUtc(TimeFormat.StepDay(sat3pm, hamburg.Tz, 1), hamburg.Tz), "day step over dst");

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
