using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace WorldClock
{
    /// <summary>Borderless dark window holding up to 3 clocks side by side. Layout and clock ticking.</summary>
    partial class MainWindow : Window
    {
        public const int MaxClocks = 3;

        readonly AppSettings settings;
        readonly ZoneEntry home;
        readonly List<ClockCard> cards = new List<ClockCard>();
        readonly StackPanel cardPanel;
        readonly ZonePicker picker = new ZonePicker();
        readonly DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render);
        readonly Button addButton;
        readonly Button pinButton;
        readonly Button lockButton;
        readonly TextBlock lockBadge;
        readonly Border shell;
        readonly FrameworkElement fullView;
        readonly CompactStrip compact = new CompactStrip();
        readonly ScaleTransform scaleTx = new ScaleTransform(1, 1);
        readonly System.Windows.Controls.Primitives.Thumb grip;
        readonly Border planBar;
        readonly Run planWhen;

        ClockCard pickingFor;   // null = adding a new clock
        DateTime? plannedUtc;   // null = live; otherwise every clock shows this instant
        ZoneEntry plannedZone;  // clock the time was set on (named in the banner)

        public event Action OptionsChanged;   // topmost/lock/clock count changed (tray menu listens)

        public int CardCount
        {
            get { return cards.Count; }
        }

        public MainWindow(AppSettings settings)
        {
            this.settings = settings;
            home = Zones.Home();

            Title = "World Clock";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.WidthAndHeight;
            ShowInTaskbar = true;
            WindowStartupLocation = WindowStartupLocation.Manual;
            FontFamily = Theme.UiFont;
            UseLayoutRounding = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Ideal);   // stays crisp when scaled
            Icon = Program.WindowIcon;

            // ---- title bar ------------------------------------------------------
            var logo = new TextBlock
            {
                Text = "\uE775",   // globe
                FontFamily = Theme.IconFont,
                FontSize = 14,
                Foreground = Theme.AccentSoftBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 8, 0)
            };
            var title = new TextBlock
            {
                Text = "World Clock",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = Theme.TextBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            lockBadge = new TextBlock
            {
                Text = "\uE72E  Locked",
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI"),
                FontSize = 10,
                Foreground = Theme.TextFaintBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 1, 0, 0)
            };

            addButton = IconButton("\uE710", "Add a clock (max " + MaxClocks + ")");
            addButton.Click += delegate { BeginAdd(); };
            pinButton = IconButton("\uE718", "");
            pinButton.Click += delegate { SetTopmost(!Topmost); };
            lockButton = IconButton("\uE785", "");
            lockButton.Click += delegate { SetLocked(!settings.Locked); };
            var menuButton = IconButton("\uE712", "More options");
            menuButton.Click += delegate
            {
                ContextMenu.PlacementTarget = menuButton;
                ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                ContextMenu.IsOpen = true;
            };
            var minButton = IconButton("\uE921", "Minimize to a one-line clock strip");
            minButton.Click += delegate { SetCompact(true); };
            var closeButton = IconButton("\uE8BB", "Hide to system tray (right-click tray icon to exit)");
            closeButton.Style = (Style)Application.Current.FindResource("CloseButton");
            closeButton.Click += delegate { HideToTray(); };

            var buttons = new StackPanel { Orientation = Orientation.Horizontal };
            foreach (var b in new[] { addButton, pinButton, lockButton, menuButton, minButton, closeButton })
                buttons.Children.Add(b);

            var titleBar = new DockPanel { Margin = new Thickness(0, 0, 0, 10), Background = Brushes.Transparent };
            DockPanel.SetDock(buttons, Dock.Right);
            titleBar.Children.Add(buttons);
            var left = new StackPanel { Orientation = Orientation.Horizontal };
            left.Children.Add(logo);
            left.Children.Add(title);
            left.Children.Add(lockBadge);
            titleBar.Children.Add(left);

            // ---- planning banner (shown while the clocks show a chosen time) ------
            var nowButton = new Button
            {
                Content = "Back to now",
                ToolTip = "Return to the live time (Esc)",
                Style = (Style)Application.Current.FindResource("PillButton")
            };
            nowButton.Click += delegate { BackToNow(); };
            var planText = new TextBlock
            {
                FontSize = 12,
                Foreground = Theme.TextBrush,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            planText.Inlines.Add(new Run("  ") { FontFamily = Theme.IconFont, Foreground = Theme.AccentSoftBrush });
            planText.Inlines.Add(new Run("Planning  ") { FontWeight = FontWeights.SemiBold, Foreground = Theme.AccentSoftBrush });
            planWhen = new Run();
            planText.Inlines.Add(planWhen);
            var planRow = new DockPanel();
            DockPanel.SetDock(nowButton, Dock.Right);
            planRow.Children.Add(nowButton);
            planRow.Children.Add(planText);
            planBar = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 5, 5, 5),
                Margin = new Thickness(0, 0, 0, 10),
                Background = Theme.Solid(Theme.Pill),
                Visibility = Visibility.Collapsed,
                Child = planRow
            };

            // ---- clocks ---------------------------------------------------------
            cardPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var content = new DockPanel();
            DockPanel.SetDock(titleBar, Dock.Top);
            DockPanel.SetDock(planBar, Dock.Top);
            content.Children.Add(titleBar);
            content.Children.Add(planBar);
            content.Children.Add(cardPanel);

            fullView = content;
            compact.ExpandRequested += delegate { SetCompact(false); };

            grip = new System.Windows.Controls.Primitives.Thumb
            {
                Style = (Style)Application.Current.FindResource("GripThumb"),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            InitResizeGrip();

            var layers = new Grid();
            layers.Children.Add(fullView);
            layers.Children.Add(compact);
            layers.Children.Add(grip);

            var root = shell = new Border
            {
                Margin = new Thickness(14),   // room for the drop shadow
                Padding = new Thickness(12, 10, 12, 12),
                CornerRadius = new CornerRadius(16),
                LayoutTransform = scaleTx,
                Background = Theme.Vertical(Theme.BgTop, Theme.BgBottom),
                BorderBrush = Theme.Solid(Theme.WindowBorder),
                BorderThickness = new Thickness(1),
                Effect = new DropShadowEffect { BlurRadius = 16, ShadowDepth = 3, Opacity = 0.55, Color = Colors.Black },
                Child = layers
            };
            Content = root;

            root.MouseLeftButtonDown += OnDragArea;
            picker.Picked += OnZonePicked;
            KeyDown += (s, e) => { if (e.Key == Key.Escape && plannedUtc.HasValue) { BackToNow(); e.Handled = true; } };

            // ---- initial clocks -------------------------------------------------
            AddCard(home, true);
            if (settings.FirstRun)
            {
                var hamburg = Zones.FindCity("Hamburg");
                if (hamburg != null) AddCard(hamburg, false);
            }
            else
            {
                foreach (var sz in settings.Zones)
                {
                    if (cards.Count >= MaxClocks) break;
                    var z = Zones.Find(sz);
                    if (z != null) AddCard(z, false);
                }
            }

            compact.SetZones(CurrentZones());
            ApplyScale(settings.Scale);
            ApplyMode(settings.Compact);
            Topmost = settings.Topmost;
            ContextMenu = BuildMenu();
            RefreshButtons();

            timer.Tick += delegate { Tick(); };
            Tick();
            timer.Start();

            InitBehavior();
        }

        Button IconButton(string glyph, string tip)
        {
            return new Button
            {
                Content = glyph,
                ToolTip = tip,
                Style = (Style)Application.Current.FindResource("IconButton")
            };
        }

        // ---- clocks -------------------------------------------------------------

        void AddCard(ZoneEntry zone, bool isHome)
        {
            var card = new ClockCard(zone, isHome);
            if (cards.Count > 0) card.Margin = new Thickness(10, 0, 0, 0);
            card.PickRequested += c => { pickingFor = c; picker.Show(c.PickAnchor); };
            card.RemoveRequested += RemoveCard;
            card.PlanRequested += (c, utc) => PlanAt(c.Zone, utc);
            cards.Add(card);
            cardPanel.Children.Add(card);
            card.UpdateTime(plannedUtc ?? DateTime.UtcNow, home.Tz, plannedUtc.HasValue);
        }

        /// <summary>Freeze every clock at one instant, e.g. "3 PM in Hamburg", to see it in the other zones.</summary>
        void PlanAt(ZoneEntry zone, DateTime utc)
        {
            plannedUtc = utc;
            plannedZone = zone;
            planBar.Visibility = Visibility.Visible;
            Tick();
        }

        public void BackToNow()
        {
            if (!plannedUtc.HasValue) return;
            plannedUtc = null;
            planBar.Visibility = Visibility.Collapsed;
            Tick();
        }

        void RemoveCard(ClockCard card)
        {
            if (card.IsHome) return;
            cards.Remove(card);
            cardPanel.Children.Remove(card);
            for (int i = 0; i < cards.Count; i++) cards[i].Margin = new Thickness(i == 0 ? 0 : 10, 0, 0, 0);
            compact.SetZones(CurrentZones());
            SaveZones();
            RefreshButtons();
        }

        public void BeginAdd()
        {
            if (cards.Count >= MaxClocks) return;
            ShowFromTray();
            if (settings.Compact) SetCompact(false);
            pickingFor = null;
            picker.Show(addButton);
        }

        void OnZonePicked(ZoneEntry zone)
        {
            if (pickingFor != null) pickingFor.SetZone(zone);
            else if (cards.Count < MaxClocks) AddCard(zone, false);
            pickingFor = null;
            compact.SetZones(CurrentZones());
            SaveZones();
            RefreshButtons();
            Tick();
        }

        void SaveZones()
        {
            settings.Zones.Clear();
            foreach (var c in cards)
                if (!c.IsHome) settings.Zones.Add(new SavedZone { TzId = c.Zone.TzId, City = c.Zone.City, Country = c.Zone.Country });
            settings.FirstRun = false;
            settings.Save();
        }

        void Tick()
        {
            var now = DateTime.UtcNow;
            if (settings.Compact) compact.Update(now, home.Tz);
            else foreach (var c in cards) c.UpdateTime(plannedUtc ?? now, home.Tz, plannedUtc.HasValue);
            if (plannedUtc.HasValue)
                planWhen.Text = TimeZoneInfo.ConvertTimeFromUtc(plannedUtc.Value, plannedZone.Tz)
                    .ToString("ddd, MMM d · h:mm tt", TimeFormat.En) + " in " + plannedZone.City;
            // Re-align to just after the next whole second so seconds never skip or stutter.
            timer.Interval = TimeSpan.FromMilliseconds(1000 - now.Millisecond + 15);
        }

        void RefreshButtons()
        {
            addButton.IsEnabled = cards.Count < MaxClocks;
            addButton.ToolTip = cards.Count < MaxClocks ? "Add a clock (max " + MaxClocks + ")" : "Maximum of " + MaxClocks + " clocks";

            pinButton.Content = Topmost ? "\uE840" : "\uE718";   // filled pin vs outline
            pinButton.Foreground = Topmost ? Theme.AccentSoftBrush : Theme.TextDimBrush;
            pinButton.ToolTip = Topmost ? "Always on top: ON (click to let other windows cover it)" : "Always on top: OFF (click to keep above other windows)";

            lockButton.Content = settings.Locked ? "\uE72E" : "\uE785";   // lock vs unlock
            lockButton.Foreground = settings.Locked ? Theme.AccentSoftBrush : Theme.TextDimBrush;
            lockButton.ToolTip = settings.Locked ? "Position locked (click to unlock)" : "Lock position here";
            lockBadge.Visibility = settings.Locked ? Visibility.Visible : Visibility.Collapsed;

            SyncMenu();
            if (OptionsChanged != null) OptionsChanged();
        }

        public void SetTopmost(bool on)
        {
            Topmost = on;
            settings.Topmost = on;
            settings.Save();
            RefreshButtons();
        }

        public void SetLocked(bool on)
        {
            settings.Locked = on;
            SavePosition();
            RefreshButtons();
        }

        public bool IsLocked
        {
            get { return settings.Locked; }
        }

        void OnDragArea(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed) return;
            if (e.OriginalSource is DependencyObject && IsInsideButton((DependencyObject)e.OriginalSource)) return;
            if (settings.Compact && e.ClickCount == 2) { SetCompact(false); e.Handled = true; return; }
            if (settings.Locked) return;
            try { DragMove(); } catch (InvalidOperationException) { }
            SavePosition();
        }

        static bool IsInsideButton(DependencyObject d)
        {
            while (d != null)
            {
                if (d is System.Windows.Controls.Primitives.ButtonBase || d is System.Windows.Controls.Primitives.Thumb) return true;
                d = d is Visual ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
            }
            return false;
        }
    }
}
