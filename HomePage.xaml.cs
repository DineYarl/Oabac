using Oabac.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

namespace Oabac
{
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();
            this.Loaded += OnHomePageLoaded;
        }

        private void OnHomePageLoaded(object sender, RoutedEventArgs e)
        {
            LoadMappings();
        }

        private void LoadMappings()
        {
            var mappings = App.Settings.Mappings;
            MappingsListView.ItemsSource = mappings;
            if (mappings != null && mappings.Any())
            {
                NoMappingsText.Visibility = Visibility.Collapsed;
                MappingsListView.Visibility = Visibility.Visible;
            }
            else
            {
                NoMappingsText.Visibility = Visibility.Visible;
                MappingsListView.Visibility = Visibility.Collapsed;
            }
        }

        private void OnSyncNowClicked(object sender, RoutedEventArgs e)
        {
            App.SyncService.RunManualSync();
        }
    }
}
