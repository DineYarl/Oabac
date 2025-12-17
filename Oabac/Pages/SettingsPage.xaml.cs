using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Oabac.Services;
using System;

namespace Oabac.Pages
{
    public sealed partial class SettingsPage : Page
    {
        private bool _isInitialized = false;

        public SettingsPage()
        {
            this.InitializeComponent();
            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            var service = App.SyncService;
            
            // Sync Mode
            SyncModeCombo.SelectedIndex = (int)service.Mode;
            
            // Sync Interval
            if (service.IntervalMinutes == 30) SyncIntervalCombo.SelectedIndex = 0;
            else if (service.IntervalMinutes == 60) SyncIntervalCombo.SelectedIndex = 1;
            else if (service.IntervalMinutes == 120) SyncIntervalCombo.SelectedIndex = 2;
            else SyncIntervalCombo.SelectedIndex = 1;

            // Mirror Deletions
            MirrorDeletionsToggle.IsOn = service.MirrorDeletions;

            // Use Recycle Bin
            UseRecycleBinToggle.IsOn = service.UseRecycleBin;

            // Run at Startup
            RunAtStartupToggle.IsOn = service.RunAtStartup;

            // Minimize to Tray
            MinimizeToTrayToggle.IsOn = service.MinimizeToTray;

            // Update UI state
            SyncIntervalCombo.IsEnabled = service.Mode == SyncMode.Interval;
            
            _isInitialized = true;
        }

        private void SaveSettings()
        {
            if (!_isInitialized) return;

            var mode = (SyncMode)SyncModeCombo.SelectedIndex;
            
            int interval = 60;
            if (SyncIntervalCombo.SelectedIndex == 0) interval = 30;
            else if (SyncIntervalCombo.SelectedIndex == 1) interval = 60;
            else if (SyncIntervalCombo.SelectedIndex == 2) interval = 120;

            bool mirror = MirrorDeletionsToggle.IsOn;
            bool minimizeToTray = MinimizeToTrayToggle.IsOn;
            bool useRecycleBin = UseRecycleBinToggle.IsOn;
            bool runAtStartup = RunAtStartupToggle.IsOn;

            App.SyncService.UpdateSettings(mode, interval, mirror, minimizeToTray, useRecycleBin, runAtStartup);
        }

        private void SyncModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SyncIntervalCombo != null)
            {
                SyncIntervalCombo.IsEnabled = SyncModeCombo.SelectedIndex == 0;
            }
            SaveSettings();
        }

        private void SyncIntervalCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveSettings();
        }

        private void MirrorDeletionsToggle_Toggled(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void UseRecycleBinToggle_Toggled(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void RunAtStartupToggle_Toggled(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void MinimizeToTrayToggle_Toggled(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }
    }
}
