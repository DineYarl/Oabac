using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Oabac.Services;
using System;
using System.IO;
using Microsoft.Windows.AppNotifications;

namespace Oabac
{
    public partial class App : Application
    {
        public static Window MainWindow { get; private set; }
        public static SyncService SyncService { get; private set; }
        public static TrayIconService TrayIconService { get; private set; }

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            try
            {
                AppNotificationManager.Default.Register();
            }
            catch (Exception ex)
            {
                // Log or ignore if notifications fail to register
                System.Diagnostics.Debug.WriteLine($"Notification registration failed: {ex.Message}");
            }

            SyncService = new SyncService();
            TrayIconService = new TrayIconService();

            string[] cmdArgs = Environment.GetCommandLineArgs();
            bool isBackground = false;
            foreach (var arg in cmdArgs)
            {
                if (arg.Equals("--background", StringComparison.OrdinalIgnoreCase))
                {
                    isBackground = true;
                    break;
                }
            }

            if (!isBackground)
            {
                MainWindow = new MainWindow();
                MainWindow.Activate();
            }
            else
            {
                // Create window but don't show it, so we have a handle for Tray Icon
                MainWindow = new MainWindow();
                // Do not call Activate()
            }
            
            // Initialize services with low priority to allow UI to render first
            MainWindow.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                try
                {
                    SyncService.Initialize();
                    TrayIconService.Initialize();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Service initialization failed: {ex.Message}");
                }
            });
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            try
            {
                AppNotificationManager.Default.Unregister();
            }
            catch { }
        }
    }
}
