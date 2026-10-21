using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace WorldClock
{
    /// <summary>Draws the app icon (blue ring, dark face, white hands) so no image files are needed.</summary>
    public static class IconArt
    {
        public static Bitmap Render(int size)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                float s = size;
                float pad = Math.Max(0.5f, s * 0.03f);
                var outer = new RectangleF(pad, pad, s - 2 * pad, s - 2 * pad);
                using (var b = new LinearGradientBrush(outer, Color.FromArgb(96, 165, 250), Color.FromArgb(29, 78, 216), 45f))
                    g.FillEllipse(b, outer);

                float ring = s * 0.09f;
                var face = RectangleF.Inflate(outer, -ring, -ring);
                using (var b = new SolidBrush(Color.FromArgb(12, 22, 44)))
                    g.FillEllipse(b, face);

                float c = s / 2f;
                using (var p = new Pen(Color.White, Math.Max(1.5f, s * 0.085f)))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    g.DrawLine(p, c, c, c, c - s * 0.27f);   // minute hand -> 12
                    g.DrawLine(p, c, c, c + s * 0.19f, c);   // hour hand -> 3
                }

                float d = Math.Max(2f, s * 0.11f);
                using (var b = new SolidBrush(Color.FromArgb(96, 165, 250)))
                    g.FillEllipse(b, c - d / 2, c - d / 2, d, d);
            }
            return bmp;
        }

        public static Icon CreateIcon(int size)
        {
            using (var bmp = Render(size))
            {
                IntPtr h = bmp.GetHicon();
                try
                {
                    using (var tmp = Icon.FromHandle(h)) return (Icon)tmp.Clone();
                }
                finally
                {
                    DestroyIcon(h);
                }
            }
        }

        /// <summary>Writes a multi-size PNG-compressed .ico (used for the .exe icon at build time).</summary>
        public static void WriteIco(string path)
        {
            int[] sizes = { 16, 24, 32, 48, 64, 256 };
            var pngs = new byte[sizes.Length][];
            for (int i = 0; i < sizes.Length; i++)
            {
                using (var bmp = Render(sizes[i]))
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    pngs[i] = ms.ToArray();
                }
            }

            using (var fs = File.Create(path))
            using (var w = new BinaryWriter(fs))
            {
                w.Write((short)0);
                w.Write((short)1);
                w.Write((short)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    byte dim = (byte)(sizes[i] >= 256 ? 0 : sizes[i]);
                    w.Write(dim);
                    w.Write(dim);
                    w.Write((byte)0);
                    w.Write((byte)0);
                    w.Write((short)1);
                    w.Write((short)32);
                    w.Write(pngs[i].Length);
                    w.Write(offset);
                    offset += pngs[i].Length;
                }
                foreach (var png in pngs) w.Write(png);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool DestroyIcon(IntPtr hIcon);
    }
}
