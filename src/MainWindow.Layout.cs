using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace WorldClock
{
    /// <summary>Resizing (uniform scale) and switching between the full view and the one-line strip.</summary>
    partial class MainWindow
    {
        bool resizing;
        POINT resizeStart;
        double resizeStartScale;
        int resizeStartW, resizeStartH;

        public bool IsCompact
        {
            get { return settings.Compact; }
        }

        public double CurrentScale
        {
            get { return settings.Scale; }
        }

        List<ZoneEntry> CurrentZones()
        {
            var list = new List<ZoneEntry>();
            foreach (var c in cards) list.Add(c.Zone);
            return list;
        }

        // ---- scale --------------------------------------------------------------

        void ApplyScale(double s)
        {
            s = AppSettings.ClampScale(s);
            settings.Scale = s;
            scaleTx.ScaleX = s;
            scaleTx.ScaleY = s;
        }

        /// <summary>Set the size from a menu, keyboard or wheel and remember it.</summary>
        public void SetScale(double s)
        {
            ApplyScale(Math.Round(s * 20) / 20);   // snap to 5% steps
            settings.Save();
            SyncMenu();
        }

        void InitResizeGrip()
        {
            grip.DragStarted += delegate
            {
                RECT r;
                if (!GetWindowRect(Hwnd, out r)) return;
                GetCursorPos(out resizeStart);
                resizeStartScale = settings.Scale;
                resizeStartW = Math.Max(1, r.Right - r.Left);
                resizeStartH = Math.Max(1, r.Bottom - r.Top);
                resizing = true;
            };
            grip.DragDelta += delegate
            {
                if (!resizing) return;
                POINT p;
                GetCursorPos(out p);
                double dx = p.X - resizeStart.X, dy = p.Y - resizeStart.Y;
                // Project the mouse movement onto the window diagonal so dragging feels natural in both
                // the tall full view and the wide one-line strip. Proportions are always kept.
                double w = resizeStartW, h = resizeStartH;
                double ratio = 1 + (dx * w + dy * h) / (w * w + h * h);
                ApplyScale(resizeStartScale * ratio);
            };
            grip.DragCompleted += delegate
            {
                if (!resizing) return;
                resizing = false;
                settings.Save();
                SyncMenu();
                KeepInsideMonitor();
            };

            PreviewMouseWheel += (s, e) =>
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == 0 || settings.Locked) return;
                SetScale(settings.Scale + (e.Delta > 0 ? 0.05 : -0.05));
                e.Handled = true;
            };
            PreviewKeyDown += (s, e) =>
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
                if (e.Key == Key.OemPlus || e.Key == Key.Add) SetScale(settings.Scale + 0.1);
                else if (e.Key == Key.OemMinus || e.Key == Key.Subtract) SetScale(settings.Scale - 0.1);
                else if (e.Key == Key.D0 || e.Key == Key.NumPad0) SetScale(1.0);
                else return;
                e.Handled = true;
            };
        }

        // ---- full / compact -----------------------------------------------------

        void ApplyMode(bool isCompact)
        {
            fullView.Visibility = isCompact ? Visibility.Collapsed : Visibility.Visible;
            compact.Visibility = isCompact ? Visibility.Visible : Visibility.Collapsed;
            if (isCompact)
            {
                shell.Padding = new Thickness(14, 7, 8, 7);
                shell.CornerRadius = new CornerRadius(12);
                compact.Margin = new Thickness(0, 0, 8, 0);   // keep the grip clear of the expand button
                grip.Margin = new Thickness(0, 0, -7, -6);
            }
            else
            {
                shell.Padding = new Thickness(12, 10, 12, 12);
                shell.CornerRadius = new CornerRadius(16);
                grip.Margin = new Thickness(0, 0, -10, -10);
            }
        }

        /// <summary>Switch between the full clocks and the one-line strip. Each view keeps its own spot.</summary>
        public void SetCompact(bool on)
        {
            if (on == settings.Compact) return;
            picker.IsOpen = false;
            timePicker.IsOpen = false;
            if (on) BackToNow();   // the one-line strip always shows the live time
            SavePosition();              // remember where the current view was
            settings.Compact = on;
            ApplyMode(on);
            Tick();
            if (!IsVisible) ShowFromTray();

            // Wait for the window to re-measure to its new size, then place it.
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate
            {
                int? x = SavedX, y = SavedY;
                if (x.HasValue && y.HasValue && IsVisibleSpot(x.Value, y.Value)) MoveTo(x.Value, y.Value);
                else KeepInsideMonitor();   // first time: open where the other view was
                SavePosition();
                RefreshButtons();
            }));
        }
    }
}
