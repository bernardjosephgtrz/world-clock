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
    /// <summary>Searchable dropdown of cities and Windows time zones.</summary>
    class ZonePicker : Popup
    {
        const int MaxResults = 250;

        public event Action<ZoneEntry> Picked;

        readonly TextBox search;
        readonly TextBlock placeholder;
        readonly ListBox list;
        readonly TextBlock empty;

        public ZonePicker()
        {
            AllowsTransparency = true;
            StaysOpen = false;
            Placement = PlacementMode.Bottom;
            VerticalOffset = 6;
            PopupAnimation = PopupAnimation.Fade;

            search = new TextBox { Style = (Style)Application.Current.FindResource("SearchBox") };
            search.TextChanged += delegate { Refresh(); };
            search.PreviewKeyDown += OnSearchKey;

            placeholder = new TextBlock
            {
                Text = "Search city, country or zone\u2026",
                Foreground = Theme.TextFaintBrush,
                FontSize = 13,
                IsHitTestVisible = false,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(32, 0, 0, 0)
            };
            var glass = new TextBlock
            {
                Text = "\uE721",
                FontFamily = Theme.IconFont,
                FontSize = 12,
                Foreground = Theme.TextDimBrush,
                IsHitTestVisible = false,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(11, 0, 0, 0)
            };
            var searchGrid = new Grid();
            searchGrid.Children.Add(search);
            searchGrid.Children.Add(glass);
            searchGrid.Children.Add(placeholder);

            list = new ListBox
            {
                Height = 300,
                Margin = new Thickness(0, 8, 0, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                ItemContainerStyle = (Style)Application.Current.FindResource("ZoneItem"),
                Focusable = false
            };
            ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
            VirtualizingStackPanel.SetIsVirtualizing(list, true);
            list.PreviewMouseLeftButtonUp += OnListClick;

            empty = new TextBlock
            {
                Text = "No matching time zones",
                Foreground = Theme.TextFaintBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 24, 0, 0),
                Visibility = Visibility.Collapsed
            };

            var listGrid = new Grid();
            listGrid.Children.Add(list);
            listGrid.Children.Add(empty);

            var root = new DockPanel();
            DockPanel.SetDock(searchGrid, Dock.Top);
            root.Children.Add(searchGrid);
            root.Children.Add(listGrid);

            var frame = new Border
            {
                Width = 330,
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

        public void Show(FrameworkElement anchor)
        {
            PlacementTarget = anchor;
            search.Text = "";
            Refresh();
            IsOpen = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(delegate
            {
                search.Focus();
                Keyboard.Focus(search);
            }));
        }

        void Refresh()
        {
            placeholder.Visibility = search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            var items = new List<ListBoxItem>();
            foreach (var z in Zones.All())
            {
                if (!Zones.Matches(z, search.Text)) continue;
                items.Add(MakeItem(z));
                if (items.Count >= MaxResults) break;
            }
            list.ItemsSource = items;
            empty.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (items.Count > 0)
            {
                list.SelectedIndex = 0;
                list.ScrollIntoView(items[0]);
            }
        }

        static ListBoxItem MakeItem(ZoneEntry z)
        {
            var off = z.Tz.GetUtcOffset(DateTime.UtcNow);
            var right = new TextBlock
            {
                Text = "UTC" + TimeFormat.Offset(off),
                FontSize = 11,
                Opacity = 0.7,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            var title = new TextBlock { Text = z.City, FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis };
            var sub = new TextBlock
            {
                Text = z.Country ?? z.Tz.Id,
                FontSize = 11,
                Opacity = 0.65,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var left = new StackPanel();
            left.Children.Add(title);
            left.Children.Add(sub);

            var row = new DockPanel();
            DockPanel.SetDock(right, Dock.Right);
            row.Children.Add(right);
            row.Children.Add(left);
            return new ListBoxItem { Content = row, Tag = z, ToolTip = z.Tz.DisplayName };
        }

        void OnSearchKey(object sender, KeyEventArgs e)
        {
            int n = list.Items.Count;
            switch (e.Key)
            {
                case Key.Down:
                    if (n > 0) list.SelectedIndex = Math.Min(n - 1, list.SelectedIndex + 1);
                    break;
                case Key.Up:
                    if (n > 0) list.SelectedIndex = Math.Max(0, list.SelectedIndex - 1);
                    break;
                case Key.PageDown:
                    if (n > 0) list.SelectedIndex = Math.Min(n - 1, list.SelectedIndex + 8);
                    break;
                case Key.PageUp:
                    if (n > 0) list.SelectedIndex = Math.Max(0, list.SelectedIndex - 8);
                    break;
                case Key.Enter:
                    Choose(list.SelectedItem as ListBoxItem);
                    break;
                case Key.Escape:
                    IsOpen = false;
                    break;
                default:
                    return;
            }
            if (list.SelectedItem != null) list.ScrollIntoView(list.SelectedItem);
            e.Handled = true;
        }

        void OnListClick(object sender, MouseButtonEventArgs e)
        {
            var d = e.OriginalSource as DependencyObject;
            while (d != null && !(d is ListBoxItem))
                d = d is Visual ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
            Choose(d as ListBoxItem);
        }

        void Choose(ListBoxItem item)
        {
            if (item == null) return;
            IsOpen = false;
            if (Picked != null) Picked((ZoneEntry)item.Tag);
        }
    }
}
