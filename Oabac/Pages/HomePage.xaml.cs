using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Oabac.Services;
using System;
using System.Threading.Tasks;

namespace Oabac.Pages
{
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();
            Loaded += HomePage_Loaded;
            Unloaded += HomePage_Unloaded;
        }

        private async void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            var mappings = App.SyncService.GetMappings();
            MappingsList.ItemsSource = mappings;
            EmptyStateText.Visibility = mappings.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            App.SyncService.StatusChanged += SyncService_StatusChanged;

            if (App.SyncService.DispatcherQueue == null)
            {
                App.SyncService.DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            }

            if (App.SyncService.IsFirstRun)
            {
                await ShowWelcomeTour();
                App.SyncService.CompleteFirstRun();
            }
        }

        private async Task ShowWelcomeTour()
        {
            var dialog = new ContentDialog
            {
                Title = "Welcome to Oabac",
                PrimaryButtonText = "Get Started",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var rootGrid = new Grid { Height = 350 };
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var flipView = new FlipView();
            var pipsPager = new PipsPager 
            { 
                NumberOfPages = 3, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                Margin = new Thickness(0, 10, 0, 0) 
            };
            
            flipView.SelectionChanged += (s, e) => pipsPager.SelectedPageIndex = flipView.SelectedIndex;
            pipsPager.SelectedIndexChanged += (s, e) => flipView.SelectedIndex = pipsPager.SelectedPageIndex;

            // Helper to create items
            UIElement CreateItem(string glyph, string title, string desc)
            {
                var panel = new StackPanel 
                { 
                    VerticalAlignment = VerticalAlignment.Center, 
                    HorizontalAlignment = HorizontalAlignment.Center, 
                    Spacing = 20,
                    Padding = new Thickness(20),
                    Transitions = new Microsoft.UI.Xaml.Media.Animation.TransitionCollection 
                    { 
                        new Microsoft.UI.Xaml.Media.Animation.EntranceThemeTransition { FromVerticalOffset = 30 } 
                    }
                };
                
                panel.Children.Add(new FontIcon 
                { 
                    Glyph = glyph, 
                    FontSize = 64, 
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"]
                });
                
                panel.Children.Add(new TextBlock 
                { 
                    Text = title, 
                    Style = (Style)Application.Current.Resources["TitleTextBlockStyle"], 
                    HorizontalAlignment = HorizontalAlignment.Center 
                });
                
                panel.Children.Add(new TextBlock 
                { 
                    Text = desc, 
                    TextWrapping = TextWrapping.Wrap, 
                    HorizontalAlignment = HorizontalAlignment.Center, 
                    TextAlignment = TextAlignment.Center,
                    Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                });
                
                return panel;
            }

            flipView.Items.Add(CreateItem("\uE895", "Sync Folders", "Easily map source and destination folders to keep them in sync."));
            flipView.Items.Add(CreateItem("\uE713", "Flexible Modes", "Choose between Real-time sync or scheduled Intervals."));
            flipView.Items.Add(CreateItem("\uF167", "Stay Informed", "Monitor progress and view detailed activity logs right on the dashboard."));

            rootGrid.Children.Add(flipView);
            rootGrid.Children.Add(pipsPager);
            
            Grid.SetRow(flipView, 0);
            Grid.SetRow(pipsPager, 1);

            dialog.Content = rootGrid;

            await dialog.ShowAsync();
        }

        private void HomePage_Unloaded(object sender, RoutedEventArgs e)
        {
            App.SyncService.StatusChanged -= SyncService_StatusChanged;
        }

        private void SyncService_StatusChanged(object sender, string e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                LogBox.Text = e + "\n" + LogBox.Text;
                if (LogBox.Text.Length > 10000)
                {
                    LogBox.Text = LogBox.Text.Substring(0, 10000);
                }
            });
        }

        private void SyncNow_Click(object sender, RoutedEventArgs e)
        {
            App.SyncService.SyncNow();
        }
    }
}
