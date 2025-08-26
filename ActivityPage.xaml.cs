using Oabac.Services;
using Microsoft.UI.Xaml.Controls;

namespace Oabac
{
    public sealed partial class ActivityPage : Page
    {
        public ActivityPage()
        {
            this.InitializeComponent();
            App.SyncService.Status += AppendLog;
        }

        private void AppendLog(string msg)
        {
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                LogBox.Text = $"{msg}\r\n{LogBox.Text}";
            });
        }
    }
}
