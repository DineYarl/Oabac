using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage.Pickers;
using Oabac.Models;

namespace Oabac.Pages
{
    public sealed partial class MappingsPage : Page
    {
        public MappingsPage()
        {
            this.InitializeComponent();
            Loaded += MappingsPage_Loaded;
        }

        private void MappingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshList();
        }

        private void RefreshList()
        {
            MappingsList.ItemsSource = null;
            MappingsList.ItemsSource = App.SyncService.GetMappings();
        }

        private async void BrowseSource_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                SourceBox.Text = folder.Path;
            }
        }

        private async void BrowseDest_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                DestBox.Text = folder.Path;
            }
        }

        private void AddMapping_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SourceBox.Text) && !string.IsNullOrWhiteSpace(DestBox.Text))
            {
                var exclusions = ExclusionsBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(s => s.Trim())
                                                   .Where(s => !string.IsNullOrEmpty(s))
                                                   .ToList();
                
                var direction = DirectionCombo.SelectedIndex == 1 ? SyncDirection.TwoWay : SyncDirection.OneWay;

                App.SyncService.AddMapping(SourceBox.Text, DestBox.Text, exclusions, direction);
                RefreshList();
                SourceBox.Text = "";
                DestBox.Text = "";
                ExclusionsBox.Text = "";
                DirectionCombo.SelectedIndex = 0;
            }
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            if (MappingsList.SelectedItem is Mapping mapping)
            {
                App.SyncService.RemoveMapping(mapping);
                RefreshList();
            }
        }
    }
}
