using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Oabac.Models
{
    public enum SyncDirection
    {
        OneWay,
        TwoWay
    }

    public class Mapping : INotifyPropertyChanged
    {
        private string _sourcePath;
        private string _destinationPath;
        private double _progress;
        private string _status;
        private string _dataTransferInfo;
        private List<string> _exclusions = new List<string>();
        private SyncDirection _direction = SyncDirection.OneWay;

        public string SourcePath
        {
            get => _sourcePath;
            set { _sourcePath = value; OnPropertyChanged(); }
        }

        public string DestinationPath
        {
            get => _destinationPath;
            set { _destinationPath = value; OnPropertyChanged(); }
        }

        public List<string> Exclusions
        {
            get => _exclusions;
            set { _exclusions = value; OnPropertyChanged(); }
        }

        public SyncDirection Direction
        {
            get => _direction;
            set { _direction = value; OnPropertyChanged(); }
        }

        public double Progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public string DataTransferInfo
        {
            get => _dataTransferInfo;
            set { _dataTransferInfo = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public object SyncLock { get; } = new object();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
