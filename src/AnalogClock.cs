using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WorldClock
{
    /// <summary>150x150 analog clock face with hour, minute and second hands.</summary>
    class AnalogClock : Canvas
    {
        const double Size = 150;
        const double C = Size / 2;

        readonly RotateTransform hourRot = new RotateTransform(0, C, C);
        readonly RotateTransform minRot = new RotateTransform(0, C, C);
        readonly RotateTransform secRot = new RotateTransform(0, C, C);

        public AnalogClock()
        {
            Width = Size;
            Height = Size;
            IsHitTestVisible = false;

            var faceFill = new RadialGradientBrush(Color.FromRgb(0x1B, 0x2F, 0x57), Color.FromRgb(0x0C, 0x16, 0x2D));
            faceFill.Freeze();
            Children.Add(new Ellipse
            {
                Width = Size,
                Height = Size,
                Fill = faceFill,
                Stroke = Theme.Solid(Color.FromRgb(0x2C, 0x42, 0x70)),
                StrokeThickness = 1.5
            });

            var major = Theme.Solid(Color.FromRgb(0xDC, 0xE6, 0xFF));
            var minor = Theme.Solid(Color.FromRgb(0x3F, 0x55, 0x82));
            for (int i = 0; i < 60; i++)
            {
                bool isMajor = i % 5 == 0;
                double a = i * 6 * Math.PI / 180;
                double r1 = 66, r2 = isMajor ? 57 : 62;
                Children.Add(new Line
                {
                    X1 = C + r1 * Math.Sin(a),
                    Y1 = C - r1 * Math.Cos(a),
                    X2 = C + r2 * Math.Sin(a),
                    Y2 = C - r2 * Math.Cos(a),
                    Stroke = isMajor ? major : minor,
                    StrokeThickness = isMajor ? 2.2 : 1,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                });
            }

            foreach (int n in new[] { 12, 3, 6, 9 })
            {
                double a = n * 30 * Math.PI / 180;
                var tb = new TextBlock
                {
                    Text = n.ToString(),
                    Width = 24,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Theme.TextDimBrush
                };
                SetLeft(tb, C + 46 * Math.Sin(a) - 12);
                SetTop(tb, C - 46 * Math.Cos(a) - 8);
                Children.Add(tb);
            }

            Children.Add(Hand(6, 34, 4.5, Theme.TextBrush, hourRot));
            Children.Add(Hand(8, 52, 3, Theme.TextBrush, minRot));
            Children.Add(Hand(14, 58, 1.4, Theme.AccentSoftBrush, secRot));

            Children.Add(Dot(9, Theme.AccentSoftBrush));
            Children.Add(Dot(3.5, Theme.Solid(Color.FromRgb(0x0C, 0x16, 0x2D))));
        }

        static Line Hand(double tail, double length, double thickness, Brush brush, RotateTransform rot)
        {
            return new Line
            {
                X1 = C,
                Y1 = C + tail,
                X2 = C,
                Y2 = C - length,
                Stroke = brush,
                StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                RenderTransform = rot
            };
        }

        static Ellipse Dot(double d, Brush brush)
        {
            var e = new Ellipse { Width = d, Height = d, Fill = brush };
            SetLeft(e, C - d / 2);
            SetTop(e, C - d / 2);
            return e;
        }

        public void SetTime(DateTime t)
        {
            double s = t.Second;
            double m = t.Minute + s / 60.0;
            double h = (t.Hour % 12) + m / 60.0;
            secRot.Angle = s * 6;
            minRot.Angle = m * 6;
            hourRot.Angle = h * 30;
        }
    }
}
