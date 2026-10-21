using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace WorldClock
{
    /// <summary>One clock: city, UTC offset, digital time on top, analog clock below.</summary>
    class ClockCard : Border
    {
        public event Action<ClockCard> PickRequested;
        public event Action<ClockCard> RemoveRequested;

        public readonly bool IsHome;
        public ZoneEntry Zone { get; private set; }

        readonly TextBlock dayIcon;
        readonly TextBlock cityText;
        readonly TextBlock subText;
        readonly TextBlock dateText;
        readonly TextBlock offsetText;
        readonly Run timeRun;
        readonly Run ampmRun;
        readonly AnalogClock analog;
        readonly FrameworkElement anchor;

        public ClockCard(ZoneEntry zone, bool isHome)
        {
            IsHome = isHome;
            Width = 224;
            CornerRadius = new CornerRadius(12);
            Padding = new Thickness(14, 12, 14, 16);
            Background = Theme.Vertical(Theme.CardTop, Theme.CardBottom);
            BorderThickness = new Thickness(1);
            BorderBrush = Theme.Solid(isHome ? Theme.HomeBorder : Theme.CardBorder);

            var stack = new StackPanel();

            var header = new DockPanel { LastChildFill = true };
            dayIcon = new TextBlock
            {
                FontFamily = Theme.IconFont,
                FontSize = 13,
                Width = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 1, 6, 0)
            };
            DockPanel.SetDock(dayIcon, Dock.Left);
            header.Children.Add(dayIcon);

            cityText = new TextBlock
            {
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = Theme.TextBrush,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (isHome)
            {
                header.Children.Add(cityText);
                anchor = cityText;
            }
            else
            {
                var remove = new Button
                {
                    Content = "\uE711",
                    Width = 22,
                    Height = 22,
                    FontSize = 9,
                    Margin = new Thickness(4, 0, 0, 0),
                    ToolTip = "Remove this clock",
                    Style = (Style)Application.Current.FindResource("IconButton")
                };
                remove.Click += delegate { if (RemoveRequested != null) RemoveRequested(this); };
                DockPanel.SetDock(remove, Dock.Right);
                header.Children.Add(remove);

                var chevron = new TextBlock
                {
                    Text = "\uE70D",
                    FontFamily = Theme.IconFont,
                    FontSize = 9,
                    Foreground = Theme.TextDimBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 2, 0, 0)
                };
                var content = new DockPanel();
                DockPanel.SetDock(chevron, Dock.Right);
                content.Children.Add(chevron);
                content.Children.Add(cityText);

                var cityButton = new Button
                {
                    Content = content,
                    ToolTip = "Change time zone",
                    Style = (Style)Application.Current.FindResource("CityButton")
                };
                cityButton.Click += delegate { if (PickRequested != null) PickRequested(this); };
                header.Children.Add(cityButton);
                anchor = cityButton;
            }
            stack.Children.Add(header);

            subText = new TextBlock
            {
                FontSize = 11,
                Foreground = Theme.TextFaintBrush,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(22, 2, 0, 0)
            };
            stack.Children.Add(subText);

            var time = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 0),
                Foreground = Theme.TextBrush
            };
            Typography.SetNumeralAlignment(time, FontNumeralAlignment.Tabular);
            timeRun = new Run { FontSize = 34, FontWeight = FontWeights.Light };
            ampmRun = new Run { FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Theme.AccentSoftBrush };
            time.Inlines.Add(timeRun);
            time.Inlines.Add(new Run(" ") { FontSize = 13 });
            time.Inlines.Add(ampmRun);
            stack.Children.Add(time);

            dateText = new TextBlock
            {
                FontSize = 12,
                Foreground = Theme.TextDimBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 0)
            };
            stack.Children.Add(dateText);

            offsetText = new TextBlock { FontSize = 11, Foreground = isHome ? Theme.TextBrush : Theme.AccentSoftBrush };
            stack.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 3, 10, 3),
                Background = Theme.Solid(isHome ? Theme.HomePill : Theme.Pill),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0),
                Child = offsetText
            });

            analog = new AnalogClock { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 14, 0, 0) };
            stack.Children.Add(analog);

            Child = stack;
            SetZone(zone);
        }

        /// <summary>Element the time zone picker drops down from.</summary>
        public FrameworkElement PickAnchor
        {
            get { return anchor; }
        }

        public void SetZone(ZoneEntry zone)
        {
            Zone = zone;
            cityText.Text = zone.City;
            cityText.ToolTip = zone.Label + "\n" + zone.Tz.DisplayName;
        }

        public void UpdateTime(DateTime utcNow, TimeZoneInfo home)
        {
            var tz = Zone.Tz;
            var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);

            timeRun.Text = local.ToString("h:mm:ss", TimeFormat.En);
            ampmRun.Text = local.ToString("tt", TimeFormat.En);
            dateText.Text = local.ToString("ddd, MMM d, yyyy", TimeFormat.En);

            string sub = "UTC" + TimeFormat.Offset(tz.GetUtcOffset(utcNow));
            if (tz.IsDaylightSavingTime(utcNow)) sub += " \u00B7 DST";
            if (!string.IsNullOrEmpty(Zone.Country)) sub = Zone.Country + " \u00B7 " + sub;
            subText.Text = sub;

            bool day = local.Hour >= 6 && local.Hour < 18;
            dayIcon.Text = day ? "\uE706" : "\uE708";
            dayIcon.Foreground = day ? Theme.SunBrush : Theme.MoonBrush;
            dayIcon.ToolTip = day ? "Daytime there" : "Night-time there";

            offsetText.Text = IsHome ? "Your local time" : TimeFormat.Describe(tz, home, utcNow);
            analog.SetTime(local);
        }
    }
}
