using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace WorldClock
{
    /// <summary>One clock: city, UTC offset, digital time on top, analog clock below.</summary>
    class ClockCard : Border
    {
        public event Action<ClockCard> PickRequested;
        public event Action<ClockCard> RemoveRequested;
        public event Action<ClockCard, DateTime> PlanRequested;   // user set a time on this clock (UTC instant)

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
        readonly TextBlock time;
        readonly TextBox editor;

        DateTime shownUtc;   // instant currently displayed (live or planned)
        int wheelAcc;        // touchpads send small deltas; step once per full notch

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

            // Digital time: click to type a time, scroll to step it. Transparent background so the whole row reacts.
            time = new TextBlock
            {
                TextAlignment = TextAlignment.Center,
                Foreground = Theme.TextBrush,
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                ToolTip = "Plan a time: click to type it (3pm, 15:00),\nor scroll ±15 min (Shift + scroll ±1 hour)"
            };
            Typography.SetNumeralAlignment(time, FontNumeralAlignment.Tabular);
            timeRun = new Run { FontSize = 34, FontWeight = FontWeights.Light };
            ampmRun = new Run { FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Theme.AccentSoftBrush };
            time.Inlines.Add(timeRun);
            time.Inlines.Add(new Run(" ") { FontSize = 13 });
            time.Inlines.Add(ampmRun);
            time.MouseLeftButtonDown += (s, e) => { BeginEdit(); e.Handled = true; };
            time.MouseWheel += (s, e) => { OnWheel(e.Delta, false); e.Handled = true; };

            editor = new TextBox
            {
                Style = (Style)Application.Current.FindResource("SearchBox"),
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 20,
                Width = 150,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
                ToolTip = "Type a time here: 3pm, 3:30 pm, 15:00\nEnter = plan it · Esc = cancel"
            };
            editor.KeyDown += OnEditorKey;
            editor.LostKeyboardFocus += delegate { EndEdit(); };

            var timeRow = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            timeRow.Children.Add(time);
            timeRow.Children.Add(editor);
            stack.Children.Add(timeRow);

            dateText = new TextBlock
            {
                FontSize = 12,
                Foreground = Theme.TextDimBrush,
                TextAlignment = TextAlignment.Center,
                Background = Brushes.Transparent,
                Margin = new Thickness(0, 1, 0, 0),
                ToolTip = "Scroll to move the planned time a day"
            };
            dateText.MouseWheel += (s, e) => { OnWheel(e.Delta, true); e.Handled = true; };
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

        // ---- planning a time -----------------------------------------------------

        void BeginEdit()
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(shownUtc, Zone.Tz);
            editor.Text = local.ToString("h:mm tt", TimeFormat.En);
            editor.Foreground = Theme.TextBrush;
            editor.Visibility = Visibility.Visible;
            time.Visibility = Visibility.Hidden;
            Dispatcher.BeginInvoke(new Action(delegate { editor.Focus(); editor.SelectAll(); }),
                System.Windows.Threading.DispatcherPriority.Input);
        }

        void EndEdit()
        {
            editor.Visibility = Visibility.Collapsed;
            time.Visibility = Visibility.Visible;
        }

        void OnEditorKey(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { EndEdit(); e.Handled = true; return; }
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            int h, m;
            if (!TimeFormat.TryParseClock(editor.Text, out h, out m))
            {
                editor.Foreground = Theme.ErrorBrush;   // stay open so it can be fixed
                editor.SelectAll();
                return;
            }
            var day = TimeZoneInfo.ConvertTimeFromUtc(shownUtc, Zone.Tz).Date;
            EndEdit();
            Plan(TimeFormat.ZoneToUtc(day.AddHours(h).AddMinutes(m), Zone.Tz));
        }

        void OnWheel(int delta, bool wholeDay)
        {
            wheelAcc += delta;
            int steps = wheelAcc / 120;
            wheelAcc %= 120;
            if (steps == 0) return;
            int dir = Math.Sign(steps);
            int minutes = (Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? 60 : 15;
            var utc = shownUtc;
            for (int i = 0; i < Math.Abs(steps); i++)
                utc = wholeDay ? TimeFormat.StepDay(utc, Zone.Tz, dir) : TimeFormat.Step(utc, Zone.Tz, minutes, dir);
            Plan(utc);
        }

        void Plan(DateTime utc)
        {
            if (PlanRequested != null) PlanRequested(this, utc);
        }

        // ---- display ------------------------------------------------------------

        /// <summary>Shows the given instant. <paramref name="planned"/> = a chosen time, not now: no seconds.</summary>
        public void UpdateTime(DateTime utcNow, TimeZoneInfo home, bool planned)
        {
            shownUtc = utcNow;
            var tz = Zone.Tz;
            var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);

            timeRun.Text = local.ToString(planned ? "h:mm" : "h:mm:ss", TimeFormat.En);
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

            offsetText.Text = IsHome ? "Your local time" : TimeFormat.Describe(tz, home, utcNow, planned);
            analog.SetTime(local, !planned);
        }
    }
}
