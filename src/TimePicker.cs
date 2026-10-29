using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace WorldClock
{
    /// <summary>Dropdown under a clock's time: type a time (3pm, 15:00) or pick one from a half-hourly list.
    /// Each entry also shows what it is in your home zone.</summary>
    class TimePicker : Popup
    {
        const int Slots = 48;                    // every half hour

        public event Action<int, int> Picked;   // hour (0-23), minute in the clock's zone

        readonly TextBox input;
        readonly ListBox list;
        readonly TextBlock hint;

        public TimePicker()
        {
            AllowsTransparency = true;
            StaysOpen = false;
            Placement = PlacementMode.Bottom;
            VerticalOffset = 4;
            PopupAnimation = PopupAnimation.Fade;

            input = new TextBox
            {
                Style = (Style)Application.Current.FindResource("SearchBox"),
                Padding = new Thickness(8, 6, 8, 6),
                FontSize = 16,
                TextAlignment = TextAlignment.Center
            };
            input.TextChanged += delegate { input.Foreground = Theme.TextBrush; };
            input.PreviewKeyDown += OnInputKey;

            hint = new TextBlock
            {
                Text = "Type 3pm, 3:30 pm or 15:00 and press Enter,\nor pick a time:",
                FontSize = 11,
                Foreground = Theme.TextFaintBrush,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 6, 0, 2)
            };

            list = new ListBox
            {
                Height = 264,
                Margin = new Thickness(0, 4, 0, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                ItemContainerStyle = (Style)Application.Current.FindResource("ZoneItem"),
                Focusable = false
            };
            ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
            list.PreviewMouseLeftButtonUp += OnListClick;

            var root = new DockPanel();
            DockPanel.SetDock(input, Dock.Top);
            DockPanel.SetDock(hint, Dock.Top);
            root.Children.Add(input);
            root.Children.Add(hint);
            root.Children.Add(list);

            var frame = new Border
            {
                Width = 236,
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 12, 12),
                CornerRadius = new CornerRadius(10),
                Background = Theme.Solid(Color.FromRgb(0x0F, 0x1A, 0x30)),
                BorderBrush = Theme.Solid(Color.FromRgb(0x2A, 0x3F, 0x6B)),
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 18,
                    ShadowDepth = 4,
                    Opacity = 0.5,
                    Color = Colors.Black
                },
                Child = root
            };
            TextElement.SetFontFamily(frame, Theme.UiFont);
            Child = frame;
        }

        /// <summary>Open under <paramref name="anchor"/> for a clock showing <paramref name="shownUtc"/> in
        /// <paramref name="zone"/>. <paramref name="home"/> = null when it is the home clock itself.</summary>
        public void Show(FrameworkElement anchor, DateTime shownUtc, TimeZoneInfo zone, ZoneEntry home)
        {
            PlacementTarget = anchor;
            HorizontalOffset = (anchor.ActualWidth - 236 - 12) / 2 + 6;   // centred under the time (frame + shadow margin)
            var local = TimeZoneInfo.ConvertTimeFromUtc(shownUtc, zone);
            input.Text = local.ToString("h:mm tt", TimeFormat.En);

            var items = new List<ListBoxItem>();
            for (int slot = 0; slot < Slots; slot++) items.Add(MakeItem(local.Date, slot * 30, zone, home));
            list.ItemsSource = items;
            int cur = local.Hour * 2 + local.Minute / 30;
            list.SelectedIndex = cur;

            IsOpen = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(delegate
            {
                list.ScrollIntoView(items[Math.Min(Slots - 1, cur + 5)]);   // a little context below
                list.ScrollIntoView(items[Math.Max(0, cur - 3)]);           // and the current time near the top
                input.Focus();
                Keyboard.Focus(input);
                input.SelectAll();
            }));
        }

        static ListBoxItem MakeItem(DateTime day, int minuteOfDay, TimeZoneInfo zone, ZoneEntry home)
        {
            var at = day.AddMinutes(minuteOfDay);
            var row = new DockPanel();
            if (home != null)
            {
                var utc = TimeFormat.ZoneToUtc(at, zone);
                var there = TimeZoneInfo.ConvertTimeFromUtc(utc, home.Tz);
                string day2 = there.Date == day ? "" : there.Date > day ? " +1d" : " −1d";
                var right = new TextBlock
                {
                    Text = there.ToString(there.Minute == 0 ? "h tt" : "h:mm tt", TimeFormat.En) + day2 + " " + home.City,
                    FontSize = 11,
                    Opacity = 0.7,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                DockPanel.SetDock(right, Dock.Right);
                row.Children.Add(right);
            }
            var left = new TextBlock { Text = at.ToString("h:mm tt", TimeFormat.En), FontSize = 13 };
            Typography.SetNumeralAlignment(left, FontNumeralAlignment.Tabular);
            row.Children.Add(left);
            return new ListBoxItem { Content = row, Tag = minuteOfDay, Focusable = false };
        }

        void OnInputKey(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                case Key.Up:
                    // Arrow keys walk the list and put the time in the box.
                    int i = Math.Max(0, Math.Min(Slots - 1, list.SelectedIndex + (e.Key == Key.Down ? 1 : -1)));
                    list.SelectedIndex = i;
                    list.ScrollIntoView(list.SelectedItem);
                    input.Text = new DateTime(2000, 1, 1).AddMinutes(i * 30).ToString("h:mm tt", TimeFormat.En);
                    input.SelectAll();
                    break;
                case Key.Enter:
                    int h, m;
                    if (TimeFormat.TryParseClock(input.Text, out h, out m)) Choose(h, m);
                    else { input.Foreground = Theme.ErrorBrush; input.SelectAll(); }   // stay open to fix it
                    break;
                case Key.Escape:
                    IsOpen = false;
                    break;
                default:
                    return;
            }
            e.Handled = true;
        }

        void OnListClick(object sender, MouseButtonEventArgs e)
        {
            var d = e.OriginalSource as DependencyObject;
            while (d != null && !(d is ListBoxItem))
                d = d is Visual ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
            var item = d as ListBoxItem;
            if (item != null) Choose((int)item.Tag / 60, (int)item.Tag % 60);
        }

        void Choose(int hour, int minute)
        {
            IsOpen = false;
            if (Picked != null) Picked(hour, minute);
        }
    }
}
