using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media; // Added for MicaBackdrop
using Oabac.Pages;
using System;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using WinRT.Interop;
using System.Runtime.InteropServices;

namespace Oabac
{
    public sealed partial class MainWindow : Window
    {
        private AppWindow _appWindow;

        public MainWindow()
        {
            this.InitializeComponent();
            
            // Enable Mica Backdrop
            SystemBackdrop = new MicaBackdrop();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            
            _appWindow = GetAppWindowForCurrentWindow();
            _appWindow.Closing += AppWindow_Closing;
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (App.SyncService.MinimizeToTray)
            {
                args.Cancel = true;
                // Hide the window
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                ShowWindow(hWnd, SW_HIDE);
            }
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;

        public void RestoreWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            ShowWindow(hWnd, SW_RESTORE);
            // Bring to front
            SetForegroundWindow(hWnd);
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
            NavView_Navigate("HomePage", new Microsoft.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo());
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                NavView_Navigate("SettingsPage", args.RecommendedNavigationTransitionInfo);
            }
            else
            {
                var selectedItem = (NavigationViewItem)args.SelectedItem;
                if (selectedItem != null)
                {
                    string pageName = selectedItem.Tag.ToString();
                    NavView_Navigate(pageName, args.RecommendedNavigationTransitionInfo);
                }
            }
        }

        private void NavView_Navigate(string navItemTag, Microsoft.UI.Xaml.Media.Animation.NavigationTransitionInfo transitionInfo)
        {
            Type pageType = null;
            if (navItemTag == "HomePage") pageType = typeof(HomePage);
            else if (navItemTag == "MappingsPage") pageType = typeof(MappingsPage);
            else if (navItemTag == "SettingsPage") pageType = typeof(SettingsPage);

            if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
            {
                ContentFrame.Navigate(pageType, null, transitionInfo);
            }
        }
    }
}
