using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Oabac
{
    public sealed partial class SettingsPage : Page
    {
        private bool _settingsLoaded = false;

        public SettingsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                var settings = App.Settings;
                
                // Set ComboBox values
                ModeCombo.SelectedIndex = settings.Mode == SyncMode.Realtime ? 1 : 0;
                IntervalCombo.SelectedIndex = settings.Interval.TotalMinutes switch
                {
                    30 => 0,    // "30 min"
                    120 => 2,   // "2 hr"
                    _ => 1      // "1 hr" (default)
                };
                
                // Set toggle switches
                MirrorDeleteCheck.IsOn = settings.MirrorDeletions;
                RunAtStartupCheck.IsOn = App.IsStartupEnabled();
                MinimizeToTrayCheck.IsOn = settings.MinimizeToTrayOnClose;
                
                UpdateIntervalEnabled();
                
                // Mark that settings are now loaded
                _settingsLoaded = true;
                
                System.Diagnostics.Debug.WriteLine($"Settings loaded - Mirror: {settings.MirrorDeletions}, Startup: {App.IsStartupEnabled()}, Tray: {settings.MinimizeToTrayOnClose}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            SaveAllSettings();
        }

        private void SaveAllSettings()
        {
            try
            {
                if (!_settingsLoaded || ModeCombo?.SelectedItem == null || 
                    IntervalCombo?.SelectedItem == null || 
                    MirrorDeleteCheck == null || 
                    MinimizeToTrayCheck == null)
                {
                    return;
                }

                var settings = App.Settings;
                settings.Mode = (ModeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Realtime" ? SyncMode.Realtime : SyncMode.Interval;
                settings.Interval = (IntervalCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
                {
                    "30 min" => TimeSpan.FromMinutes(30),
                    "2 hr" => TimeSpan.FromHours(2),
                    _ => TimeSpan.FromHours(1)
                };
                settings.MirrorDeletions = MirrorDeleteCheck.IsOn;
                settings.MinimizeToTrayOnClose = MinimizeToTrayCheck.IsOn;
                
                settings.Save();
                App.SyncService.Configure(settings);
                
                System.Diagnostics.Debug.WriteLine($"Settings saved - Mirror: {settings.MirrorDeletions}, Tray: {settings.MinimizeToTrayOnClose}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private void OnModeChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateIntervalEnabled();
            if (_settingsLoaded)
            {
                SaveAllSettings();
            }
        }

        private void UpdateIntervalEnabled()
        {
            if (ModeCombo?.SelectedItem is ComboBoxItem item && IntervalCombo != null)
            {
                IntervalCombo.IsEnabled = item.Content?.ToString() == "Interval";
            }
        }

        private void OnIntervalChanged(object sender, SelectionChangedEventArgs e) 
        { 
            if (_settingsLoaded)
            {
                SaveAllSettings();
            }
        }

        // Event handlers for toggles - TEMPORARILY REMOVE CHECKS FOR DEBUGGING
        private void OnStartupClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"*** STARTUP TOGGLE EVENT FIRED! SettingsLoaded: {_settingsLoaded} ***");
                
                if (sender is ToggleSwitch toggle)
                {
                    System.Diagnostics.Debug.WriteLine($"*** Startup IsOn: {toggle.IsOn} ***");
                    App.TrySetStartup(toggle.IsOn);
                    System.Diagnostics.Debug.WriteLine($"*** Startup setting applied: {toggle.IsOn} ***");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in startup toggle: {ex.Message}");
            }
        }

        private void OnMirrorDeleteClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"*** MIRROR DELETE TOGGLE EVENT FIRED! SettingsLoaded: {_settingsLoaded} ***");
                
                if (sender is ToggleSwitch toggle)
                {
                    System.Diagnostics.Debug.WriteLine($"*** Mirror delete IsOn: {toggle.IsOn} ***");
                    App.Settings.MirrorDeletions = toggle.IsOn;
                    App.Settings.Save();
                    App.SyncService.Configure(App.Settings);
                    System.Diagnostics.Debug.WriteLine($"*** Mirror delete setting saved: {toggle.IsOn} ***");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in mirror delete toggle: {ex.Message}");
            }
        }

        private void OnMinimizeToTrayClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"*** MINIMIZE TO TRAY TOGGLE EVENT FIRED! SettingsLoaded: {_settingsLoaded} ***");
                
                if (sender is ToggleSwitch toggle)
                {
                    System.Diagnostics.Debug.WriteLine($"*** Minimize to tray IsOn: {toggle.IsOn} ***");
                    App.Settings.MinimizeToTrayOnClose = toggle.IsOn;
                    App.Settings.Save();
                    System.Diagnostics.Debug.WriteLine($"*** Minimize to tray setting saved: {toggle.IsOn} ***");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in minimize to tray toggle: {ex.Message}");
            }
        }
    }
}
