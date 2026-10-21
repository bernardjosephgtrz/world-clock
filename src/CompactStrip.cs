using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace WorldClock
{
    /// <summary>Single-line view: "☀ Cebu 5:01:12 PM │ ☀ Hamburg 11:01:12 AM −6h │ …" plus an expand button.</summary>
    class CompactStrip : DockPanel
    {
        public event Action ExpandRequested;

        class Item
        {
            public ZoneEntry Zone;
            public bool IsHome;
            public FrameworkElement Root;
            public TextBlock Icon;
            public Run Time, AmPm, Offset;
        }

        readonly StackPanel itemsPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        readonly List<Item> items = new List<Item>();

        public CompactStrip()
        {
            LastChildFill = true;
            Background = Brushes.Transparent;   // whole strip is a drag handle

            var expand = new Button
            {
                Content = "\uE740",
                ToolTip = "Back to full view (or double-click the strip)",
                Width = 26,
                Height = 24,
                FontSize = 11,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Style = (Style)Application.Current.FindResource("IconButton")
            };
            expand.Click += delegate { if (ExpandRequested != null) ExpandRequested(); };
            DockPanel.SetDock(expand, Dock.Right);
            Children.Add(expand);
            Children.Add(itemsPanel);
        }

        public void SetZones(IList<ZoneEntry> zones)
        {
            items.Clear();
            itemsPanel.Children.Clear();
            for (int i = 0; i < zones.Count; i++)
            {
                if (i > 0)
                    itemsPanel.Children.Add(new Border
                    {
                        Width = 1,
                        Height = 18,
                        Margin = new Thickness(12, 0, 12, 0),
                        Background = Theme.Solid(Theme.CardBorder),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                var it = Make(zones[i], i == 0);
                items.Add(it);
                itemsPanel.Children.Add(it.Root);
            }
        }

        static Item Make(ZoneEntry zone, bool isHome)
        {
            var it = new Item { Zone = zone, IsHome = isHome };
            var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            it.Icon = new TextBlock
            {
                FontFamily = Theme.IconFont,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 1, 6, 0)
            };
            row.Children.Add(it.Icon);

            row.Children.Add(new TextBlock
            {
                Text = zone.City,
                FontSize = 13,
                FontWeight = isHome ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = isHome ? Theme.TextBrush : Theme.TextDimBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            });

            var time = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Theme.TextBrush };
            Typography.SetNumeralAlignment(time, FontNumeralAlignment.Tabular);
            it.Time = new Run { FontSize = 17, FontWeight = FontWeights.SemiBold };
            it.AmPm = new Run { FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Theme.AccentSoftBrush };
            it.Offset = new Run { FontSize = 11, Foreground = Theme.TextFaintBrush };
            time.Inlines.Add(it.Time);
            time.Inlines.Add(new Run(" ") { FontSize = 10 });
            time.Inlines.Add(it.AmPm);
            if (!isHome)
            {
                time.Inlines.Add(new Run("  ") { FontSize = 11 });
                time.Inlines.Add(it.Offset);
            }
            row.Children.Add(time);

            it.Root = row;
            return it;
        }

        public void Update(DateTime utcNow, TimeZoneInfo home)
        {
            foreach (var it in items)
            {
                var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, it.Zone.Tz);
                it.Time.Text = local.ToString("h:mm:ss", TimeFormat.En);
                it.AmPm.Text = local.ToString("tt", TimeFormat.En);
                bool day = local.Hour >= 6 && local.Hour < 18;
                it.Icon.Text = day ? "\uE706" : "\uE708";
                it.Icon.Foreground = day ? Theme.SunBrush : Theme.MoonBrush;
                if (!it.IsHome) it.Offset.Text = TimeFormat.Short(it.Zone.Tz, home, utcNow);
                it.Root.ToolTip = it.Zone.Label + "\n" + local.ToString("dddd, MMM d, yyyy", TimeFormat.En) +
                                  (it.IsHome ? "\nYour local time" : "\n" + TimeFormat.Describe(it.Zone.Tz, home, utcNow));
            }
        }
    }
}
