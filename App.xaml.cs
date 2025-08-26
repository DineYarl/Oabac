using Microsoft.UI.Xaml;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Oabac.Services;

namespace Oabac
{
    public partial class App : Application
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        private const int SW_HIDE = 0;

        public static SyncService SyncService { get; } = new();
        public static SyncSettings Settings { get; private set; } = new();
        public static IntPtr Hwnd { get; set; }
        private Window? _window;
        private TrayIconService? _trayIcon;

        public App()
        {
            try
            {
                this.InitializeComponent();
                
                // Load settings safely
                Settings = SyncSettings.Load();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in App constructor: {ex.Message}");
                // Continue with default settings if loading fails
                Settings = new SyncSettings();
            }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            try
            {
                // Check for background argument
                bool isBackground = args.Arguments.Contains("--background");

                if (!isBackground)
                {
                    LaunchMainWindow();
                }

                // Always configure sync service and tray icon
                SyncService.Configure(Settings);
                _trayIcon = new TrayIconService(OnTrayCommand);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnLaunched: {ex.Message}");
                
                // Try to at least show the main window if possible
                if (!args.Arguments.Contains("--background"))
                {
                    try
                    {
                        LaunchMainWindow();
                    }
                    catch (Exception innerEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error launching main window: {innerEx.Message}");
                    }
                }
            }
        }

        private void LaunchMainWindow()
        {
            try
            {
                if (_window == null)
                {
                    _window = new MainWindow();
                    _window.Activate();
                    _window.Closed += OnMainWindowClosed;
                }
                else
                {
                    _window.Activate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LaunchMainWindow: {ex.Message}");
                throw; // Re-throw to let caller handle
            }
        }

        private void OnMainWindowClosed(object sender, WindowEventArgs args)
        {
            try
            {
                // If the setting is true, prevent the window from actually closing
                if (Settings.MinimizeToTrayOnClose)
                {
                    args.Handled = true;
                    // Hide the window using Windows API
                    ShowWindow(Hwnd, SW_HIDE);
                }
                else
                {
                    // Otherwise, allow the app to exit fully.
                    ExitApplication();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnMainWindowClosed: {ex.Message}");
            }
        }

        private void OnTrayCommand(TrayCommand command)
        {
            try
            {
                switch (command)
                {
                    case TrayCommand.Open:
                        LaunchMainWindow();
                        break;
                    case TrayCommand.SyncNow:
                        SyncService.RunManualSync();
                        break;
                    case TrayCommand.Exit:
                        ExitApplication();
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnTrayCommand: {ex.Message}");
            }
        }

        private void ExitApplication()
        {
            try
            {
                _trayIcon?.Dispose();
                _trayIcon = null;

                SyncService.Dispose();

                if (_window != null)
                {
                    _window.Closed -= OnMainWindowClosed; // Unsubscribe to prevent re-entrancy
                    _window.Close();
                    _window = null;
                }

                this.Exit();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ExitApplication: {ex.Message}");
                // Force exit if normal exit fails
                Environment.Exit(0);
            }
        }

        public static void TrySetStartup(bool enable)
        {
            try
            {
                const string runKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(runKey, true) ?? Microsoft.Win32.Registry.CurrentUser.CreateSubKey(runKey);
                var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!;
                if (enable)
                {
                    // Add --background argument for startup
                    key.SetValue("Oabac", $"\"{exe}\" --background");
                }
                else
                {
                    key.DeleteValue("Oabac", false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Startup toggle failed: {ex.Message}");
            }
        }

        public static bool IsStartupEnabled()
        {
            try
            {
                const string runKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(runKey, false);
                // Check if the value exists and contains the --background argument
                return key?.GetValue("Oabac") is string value && value.Contains("--background");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking startup: {ex.Message}");
                return false;
            }
        }
    }
}
