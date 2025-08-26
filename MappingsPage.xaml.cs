using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.Collections.ObjectModel;

namespace Oabac
{
    public sealed partial class MappingsPage : Page
    {
        public ObservableCollection<Mapping> Mappings { get; } = new();

        public MappingsPage()
        {
            this.InitializeComponent();
            LoadMappings();
        }

        private void LoadMappings()
        {
            Mappings.Clear();
            foreach (var m in App.Settings.Mappings)
            {
                Mappings.Add(m);
            }
        }

        private async void OnBrowseSource(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            InitializeWithWindow.Initialize(picker, App.Hwnd);
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null) SourceText.Text = folder.Path;
        }

        private async void OnBrowseDest(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            InitializeWithWindow.Initialize(picker, App.Hwnd);
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null) DestText.Text = folder.Path;
        }

        private void OnAddMapping(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SourceText.Text) || string.IsNullOrWhiteSpace(DestText.Text)) return;
            var newMapping = new Mapping { SourceFolder = SourceText.Text, DestinationFolder = DestText.Text };
            if (!Mappings.Any(m => m.SourceFolder == newMapping.SourceFolder && m.DestinationFolder == newMapping.DestinationFolder))
            {
                Mappings.Add(newMapping);
                App.Settings.Mappings = Mappings.ToList();
                App.Settings.Save();
                App.SyncService.Configure(App.Settings); // Re-configure service
            }
        }

        private void OnRemoveMapping(object sender, RoutedEventArgs e)
        {
            if (MappingList.SelectedItem is Mapping selected)
            {
                Mappings.Remove(selected);
                App.Settings.Mappings = Mappings.ToList();
                App.Settings.Save();
                App.SyncService.Configure(App.Settings); // Re-configure service
            }
        }
    }
}
