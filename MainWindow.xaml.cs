using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Media;

namespace Oabac
{
    public sealed partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;

        public MainWindow()
        {
            this.InitializeComponent();
            App.Hwnd = WindowNative.GetWindowHandle(this);
            InitializeModernWindow();
            DisableDoubleClickMaximize();
        }

        private void DisableDoubleClickMaximize()
        {
            try
            {
                // Remove the maximize box style to prevent double-click maximize
                var hwnd = App.Hwnd;
                var style = GetWindowLong(hwnd, GWL_STYLE);
                style &= ~WS_MAXIMIZEBOX;
                SetWindowLong(hwnd, GWL_STYLE, style);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to disable double-click maximize: {ex.Message}");
            }
        }

        private void InitializeModernWindow()
        {
            Title = "Oabac";
            ExtendsContentIntoTitleBar = true;
            
            // Set the custom title bar
            SetTitleBar(AppTitleBar);

            // Configure modern title bar appearance
            var appWindow = GetAppWindow();
            if (appWindow?.TitleBar != null)
            {
                var titleBar = appWindow.TitleBar;
                
                // Use system theme colors for modern appearance
                titleBar.BackgroundColor = Microsoft.UI.Colors.Transparent;
                titleBar.ForegroundColor = Microsoft.UI.Colors.Transparent;
                titleBar.InactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
                titleBar.InactiveForegroundColor = Microsoft.UI.Colors.Transparent;
                
                // Modern button styling
                titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                titleBar.ButtonForegroundColor = Microsoft.UI.Colors.Transparent;
                titleBar.ButtonHoverBackgroundColor = Microsoft.UI.Colors.Transparent;
                titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.Transparent;
                titleBar.ButtonPressedBackgroundColor = Microsoft.UI.Colors.Transparent;
                titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
                titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.Transparent;
            }

            // Apply Mica backdrop for modern Windows 11 appearance
            SystemBackdrop = new MicaBackdrop();

            // Set window size
            appWindow?.Resize(new Windows.Graphics.SizeInt32(850, 680));

            if (appWindow?.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsMaximizable = false;
                presenter.IsResizable = false;
            }
            
            // Navigate to the home page by default
            NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().First();
            NavView.IsPaneOpen = false;
        }

        private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
            }
            else if (args.SelectedItemContainer is NavigationViewItem item && item.Tag is string tagString)
            {
                var pageType = Type.GetType(tagString);
                if (pageType != null)
                {
                    ContentFrame.Navigate(pageType);
                }
            }
        }

        private void OnNavItemDoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            // Prevent double-tap from maximizing window
            e.Handled = true;
        }

        private AppWindow? GetAppWindow()
        {
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(App.Hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }
    }
}
