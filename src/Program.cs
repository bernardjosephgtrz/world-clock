using System;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

namespace WorldClock
{
    static class Program
    {
        const string MutexName = @"Local\WorldClock-SingleInstance-7f3c";
        const string ShowEventName = @"Local\WorldClock-Show-7f3c";

        static Application app;
        static MainWindow window;
        static Forms.NotifyIcon tray;
        static Forms.ToolStripMenuItem trayShow, trayTop, trayLock, trayAdd, trayCompact;
        static bool hintShown;

        public static ImageSource WindowIcon;

        [STAThread]
        static int Main(string[] args)
        {
            bool created;
            using (var mutex = new Mutex(true, MutexName, out created))
            using (var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName))
            {
                if (!created)
                {
                    // Already running: ask the existing instance to show itself, then quit.
                    showEvent.Set();
                    return 0;
                }

                app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                app.Resources.MergedDictionaries.Add(Theme.LoadStyles());
                app.SessionEnding += delegate { Exit(); };

                using (var ico = IconArt.CreateIcon(32))
                {
                    WindowIcon = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    WindowIcon.Freeze();
                }

                window = new MainWindow(AppSettings.Load());
                CreateTray();
                window.OptionsChanged += SyncTray;
                SyncTray();

                // Listen for "show yourself" requests from a second launch.
                ThreadPool.RegisterWaitForSingleObject(showEvent, delegate
                {
                    app.Dispatcher.BeginInvoke(new Action(delegate { window.ShowFromTray(); }));
                }, null, Timeout.Infinite, false);

                window.Show();
                app.Run();

                tray.Visible = false;
                tray.Dispose();
                GC.KeepAlive(mutex);
            }
            return 0;
        }

        static void CreateTray()
        {
            var menu = new Forms.ContextMenuStrip();
            trayShow = new Forms.ToolStripMenuItem("Hide clock", null, delegate { window.ToggleVisible(); });
            trayShow.Font = new System.Drawing.Font(trayShow.Font, System.Drawing.FontStyle.Bold);
            trayAdd = new Forms.ToolStripMenuItem("Add clock\u2026", null, delegate { window.BeginAdd(); });
            trayCompact = new Forms.ToolStripMenuItem("One-line view", null, delegate { window.SetCompact(!window.IsCompact); });
            trayTop = new Forms.ToolStripMenuItem("Always on top", null, delegate { window.SetTopmost(!window.Topmost); });
            trayLock = new Forms.ToolStripMenuItem("Lock position", null, delegate { window.SetLocked(!window.IsLocked); });
            menu.Items.Add(trayShow);
            menu.Items.Add(trayAdd);
            menu.Items.Add(trayCompact);
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add(trayTop);
            menu.Items.Add(trayLock);
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add(new Forms.ToolStripMenuItem("Exit World Clock", null, delegate { Exit(); }));
            menu.Opening += delegate { SyncTray(); };

            tray = new Forms.NotifyIcon
            {
                Icon = IconArt.CreateIcon(Forms.SystemInformation.SmallIconSize.Width),
                Text = "World Clock",
                ContextMenuStrip = menu,
                Visible = true
            };
            tray.MouseClick += (s, e) =>
            {
                if (e.Button == Forms.MouseButtons.Left) window.ToggleVisible();
            };
        }

        static void SyncTray()
        {
            if (trayShow == null) return;
            trayShow.Text = window.IsVisible && window.WindowState != WindowState.Minimized ? "Hide clock" : "Show clock";
            trayTop.Checked = window.Topmost;
            trayLock.Checked = window.IsLocked;
            trayCompact.Checked = window.IsCompact;
            trayAdd.Enabled = window.CardCount < MainWindow.MaxClocks;
        }

        /// <summary>First time the window is hidden, tell the user where it went.</summary>
        public static void NotifyHiddenOnce()
        {
            if (hintShown || tray == null) return;
            hintShown = true;
            tray.ShowBalloonTip(3000, "World Clock is still running",
                "Click the tray icon to bring it back. Right-click it to exit.", Forms.ToolTipIcon.None);
        }

        public static void Exit()
        {
            if (window != null) window.ExitNow();
            if (app != null) app.Shutdown();
        }
    }
}
