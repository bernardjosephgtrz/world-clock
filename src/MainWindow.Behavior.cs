using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

namespace WorldClock
{
    /// <summary>Window placement (pin/lock/snap/multi-monitor), right-click menu, hide/show.</summary>
    partial class MainWindow
    {
        MenuItem miTop, miLock, miAdd, miNextMonitor, miCompact;
        readonly System.Collections.Generic.List<MenuItem> miSizes = new System.Collections.Generic.List<MenuItem>();
        bool exiting;

        void InitBehavior()
        {
            Loaded += delegate { RestorePosition(); };
            SizeChanged += delegate { if (IsLoaded) Dispatcher.BeginInvoke(new Action(KeepInsideMonitor)); };
            Closing += OnClosing;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
            Microsoft.Win32.SystemEvents.TimeChanged += OnSystemTimeChanged;
        }

        IntPtr Hwnd
        {
            get { return new WindowInteropHelper(this).Handle; }
        }

        // ---- menu ---------------------------------------------------------------

        ContextMenu BuildMenu()
        {
            var m = new ContextMenu();
            miAdd = Item("Add clock\u2026", delegate { BeginAdd(); });
            miTop = Item("Always on top", delegate { SetTopmost(!Topmost); });
            miTop.IsCheckable = true;
            miLock = Item("Lock position", delegate { SetLocked(!settings.Locked); });
            miLock.IsCheckable = true;

            var move = new MenuItem { Header = "Move to" };
            move.Items.Add(Item("Top-left corner", delegate { SnapTo(0, 0); }));
            move.Items.Add(Item("Top-right corner", delegate { SnapTo(1, 0); }));
            move.Items.Add(Item("Bottom-left corner", delegate { SnapTo(0, 1); }));
            move.Items.Add(Item("Bottom-right corner", delegate { SnapTo(1, 1); }));
            move.Items.Add(Item("Center of screen", delegate { SnapTo(0.5, 0.5); }));
            move.Items.Add(new Separator());
            miNextMonitor = Item("Next monitor", delegate { MoveToNextMonitor(); });
            move.Items.Add(miNextMonitor);

            miCompact = Item("One-line view", delegate { SetCompact(!settings.Compact); });
            miCompact.IsCheckable = true;

            var size = new MenuItem { Header = "Size" };
            size.Items.Add(Item("Larger\tCtrl +", delegate { SetScale(settings.Scale + 0.1); }));
            size.Items.Add(Item("Smaller\tCtrl \u2212", delegate { SetScale(settings.Scale - 0.1); }));
            size.Items.Add(new Separator());
            miSizes.Clear();
            foreach (var p in new[] { 0.75, 1.0, 1.25, 1.5, 2.0 })
            {
                double v = p;
                var mi = Item((int)(v * 100) + "%" + (v == 1.0 ? "  (normal)\tCtrl 0" : ""), delegate { SetScale(v); });
                mi.IsCheckable = true;
                mi.Tag = v;
                miSizes.Add(mi);
                size.Items.Add(mi);
            }

            m.Items.Add(miAdd);
            m.Items.Add(miCompact);
            m.Items.Add(new Separator());
            m.Items.Add(miTop);
            m.Items.Add(miLock);
            m.Items.Add(move);
            m.Items.Add(size);
            m.Items.Add(new Separator());
            m.Items.Add(Item("Hide to system tray", delegate { HideToTray(); }));
            m.Items.Add(Item("Exit World Clock", delegate { Program.Exit(); }));
            m.Opened += delegate { SyncMenu(); };
            return m;
        }

        static MenuItem Item(string header, RoutedEventHandler click)
        {
            var mi = new MenuItem { Header = header };
            mi.Click += click;
            return mi;
        }

        void SyncMenu()
        {
            if (miTop == null) return;
            miTop.IsChecked = Topmost;
            miLock.IsChecked = settings.Locked;
            miAdd.IsEnabled = cards.Count < MaxClocks;
            miNextMonitor.IsEnabled = Forms.Screen.AllScreens.Length > 1;
            miCompact.IsChecked = settings.Compact;
            foreach (var mi in miSizes) mi.IsChecked = Math.Abs((double)mi.Tag - settings.Scale) < 0.001;
            grip.Visibility = settings.Locked ? Visibility.Collapsed : Visibility.Visible;
        }

        // ---- show / hide --------------------------------------------------------

        public void HideToTray()
        {
            picker.IsOpen = false;
            timePicker.IsOpen = false;
            Hide();
            Program.NotifyHiddenOnce();
        }

        public void ShowFromTray()
        {
            if (!IsVisible) Show();
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            EnsureOnScreen();
            Activate();
        }

        public void ToggleVisible()
        {
            if (IsVisible && WindowState != WindowState.Minimized) HideToTray();
            else ShowFromTray();
        }

        public void ExitNow()
        {
            exiting = true;
            Close();
        }

        void OnClosing(object sender, CancelEventArgs e)
        {
            if (!exiting)
            {
                // Alt+F4 / taskbar close: keep running in the tray instead of quitting.
                e.Cancel = true;
                HideToTray();
                return;
            }
            timer.Stop();
            if (!settings.Locked) SavePosition();
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
            Microsoft.Win32.SystemEvents.TimeChanged -= OnSystemTimeChanged;
        }

        // ---- positioning (physical pixels, so it works across monitors) ----------

        /// <summary>Saved spot for the view currently shown (full and compact are remembered separately).</summary>
        int? SavedX
        {
            get { return settings.Compact ? settings.CX : settings.X; }
        }

        int? SavedY
        {
            get { return settings.Compact ? settings.CY : settings.Y; }
        }

        void RestorePosition()
        {
            if (SavedX.HasValue && SavedY.HasValue && IsVisibleSpot(SavedX.Value, SavedY.Value))
                MoveTo(SavedX.Value, SavedY.Value);
            else
                SnapTo(1, 0, Forms.Screen.PrimaryScreen, false);   // default: top-right of main monitor
        }

        public void SavePosition()
        {
            RECT r;
            if (Hwnd == IntPtr.Zero || !GetWindowRect(Hwnd, out r)) return;
            if (settings.Compact) { settings.CX = r.Left; settings.CY = r.Top; }
            else { settings.X = r.Left; settings.Y = r.Top; }
            settings.Save();
        }

        void SnapTo(double fx, double fy)
        {
            SnapTo(fx, fy, Forms.Screen.FromHandle(Hwnd), true);
        }

        void SnapTo(double fx, double fy, Forms.Screen screen, bool save)
        {
            RECT r;
            if (!GetWindowRect(Hwnd, out r)) return;
            int w = r.Right - r.Left, h = r.Bottom - r.Top;
            var wa = screen.WorkingArea;
            int x = wa.Left + (int)Math.Round((wa.Width - w) * fx);
            int y = wa.Top + (int)Math.Round((wa.Height - h) * fy);
            MoveTo(x, y);
            if (save) SavePosition();
        }

        void MoveToNextMonitor()
        {
            var all = Forms.Screen.AllScreens;
            if (all.Length < 2) return;
            var cur = Forms.Screen.FromHandle(Hwnd);
            int i = Array.IndexOf(all, cur);
            for (int k = 0; k < all.Length; k++)
                if (all[k].DeviceName == cur.DeviceName) i = k;
            SnapTo(1, 0, all[(i + 1) % all.Length], true);
        }

        void MoveTo(int x, int y)
        {
            SetWindowPos(Hwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        }

        /// <summary>True if the top-left part of a window at (x,y) would be on some monitor.</summary>
        static bool IsVisibleSpot(int x, int y)
        {
            var probe = new System.Drawing.Rectangle(x + 20, y + 20, 120, 40);
            foreach (var s in Forms.Screen.AllScreens)
                if (s.WorkingArea.IntersectsWith(probe)) return true;
            return false;
        }

        void EnsureOnScreen()
        {
            RECT r;
            if (Hwnd == IntPtr.Zero || !GetWindowRect(Hwnd, out r)) return;

            // Saved spot is back (e.g. office monitors re-attached): return to it.
            if (SavedX.HasValue && SavedY.HasValue && IsVisibleSpot(SavedX.Value, SavedY.Value))
            {
                if (r.Left != SavedX.Value || r.Top != SavedY.Value) MoveTo(SavedX.Value, SavedY.Value);
                return;
            }
            // Otherwise, if the window is now off every screen (e.g. laptop undocked), bring it to the main
            // monitor without overwriting the saved spot.
            if (!IsVisibleSpot(r.Left, r.Top)) SnapTo(1, 0, Forms.Screen.PrimaryScreen, false);
        }

        /// <summary>When the window grows (clock added), shift it so it doesn't spill off its monitor.</summary>
        void KeepInsideMonitor()
        {
            RECT r;
            if (resizing || Hwnd == IntPtr.Zero || !GetWindowRect(Hwnd, out r)) return;
            var wa = Forms.Screen.FromHandle(Hwnd).WorkingArea;
            int x = Math.Max(wa.Left, Math.Min(r.Left, wa.Right - (r.Right - r.Left)));
            int y = Math.Max(wa.Top, Math.Min(r.Top, wa.Bottom - (r.Bottom - r.Top)));
            if (x == r.Left && y == r.Top) return;
            MoveTo(x, y);
            SavePosition();
        }

        void OnDisplayChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(EnsureOnScreen));
        }

        void OnSystemTimeChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(Tick));
        }

        // ---- Win32 --------------------------------------------------------------

        const uint SWP_NOSIZE = 0x0001, SWP_NOZORDER = 0x0004, SWP_NOACTIVATE = 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int X, Y; }

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT p);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    }
}
