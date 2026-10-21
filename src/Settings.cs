using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace WorldClock
{
    public class SavedZone
    {
        public string TzId;
        public string City;
        public string Country;
    }

    /// <summary>Simple key=value settings stored in %APPDATA%\WorldClock\settings.ini.</summary>
    public class AppSettings
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public int? X;               // full view position in physical pixels
        public int? Y;
        public int? CX;              // compact (one-line) view position
        public int? CY;
        public bool Compact;
        public double Scale = 1.0;
        public bool Topmost;
        public bool Locked;
        public bool FirstRun = true;
        public List<SavedZone> Zones = new List<SavedZone>();

        public const double MinScale = 0.6, MaxScale = 2.0;

        public static double ClampScale(double s)
        {
            if (double.IsNaN(s) || double.IsInfinity(s)) return 1.0;
            return Math.Max(MinScale, Math.Min(MaxScale, s));
        }

        static string Dir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WorldClock"); }
        }

        public static string FilePath
        {
            get { return Path.Combine(Dir, "settings.ini"); }
        }

        public static AppSettings Load()
        {
            var s = new AppSettings();
            try
            {
                if (!File.Exists(FilePath)) return s;
                s.FirstRun = false;
                foreach (var raw in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line[0] == '#') continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    var key = line.Substring(0, eq).Trim().ToLowerInvariant();
                    var val = line.Substring(eq + 1).Trim();
                    int iv; bool bv; double dv;
                    switch (key)
                    {
                        case "x": if (int.TryParse(val, NumberStyles.Integer, Inv, out iv)) s.X = iv; break;
                        case "y": if (int.TryParse(val, NumberStyles.Integer, Inv, out iv)) s.Y = iv; break;
                        case "cx": if (int.TryParse(val, NumberStyles.Integer, Inv, out iv)) s.CX = iv; break;
                        case "cy": if (int.TryParse(val, NumberStyles.Integer, Inv, out iv)) s.CY = iv; break;
                        case "compact": if (bool.TryParse(val, out bv)) s.Compact = bv; break;
                        case "scale":
                            if (double.TryParse(val, NumberStyles.Float, Inv, out dv)) s.Scale = ClampScale(dv);
                            break;
                        case "topmost": if (bool.TryParse(val, out bv)) s.Topmost = bv; break;
                        case "locked": if (bool.TryParse(val, out bv)) s.Locked = bv; break;
                        case "zone":
                            var p = val.Split('|');
                            if (p.Length >= 2)
                                s.Zones.Add(new SavedZone { TzId = p[0], City = p[1], Country = p.Length > 2 ? p[2] : "" });
                            break;
                    }
                }
            }
            catch (Exception)
            {
                // Unreadable settings: fall back to defaults rather than failing to start.
            }
            return s;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var sb = new StringBuilder();
                sb.AppendLine("# World Clock settings");
                if (X.HasValue) sb.AppendLine("x=" + X.Value.ToString(Inv));
                if (Y.HasValue) sb.AppendLine("y=" + Y.Value.ToString(Inv));
                if (CX.HasValue) sb.AppendLine("cx=" + CX.Value.ToString(Inv));
                if (CY.HasValue) sb.AppendLine("cy=" + CY.Value.ToString(Inv));
                sb.AppendLine("compact=" + Compact.ToString().ToLowerInvariant());
                sb.AppendLine("scale=" + Math.Round(Scale, 3).ToString(Inv));
                sb.AppendLine("topmost=" + Topmost.ToString().ToLowerInvariant());
                sb.AppendLine("locked=" + Locked.ToString().ToLowerInvariant());
                foreach (var z in Zones)
                    sb.AppendLine("zone=" + z.TzId + "|" + z.City + "|" + (z.Country ?? ""));
                var tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(false));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch (Exception)
            {
                // Not being able to save settings should never crash the clock.
            }
        }
    }
}
