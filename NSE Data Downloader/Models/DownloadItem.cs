using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace NSE_Data_Downloader.Models
{
    public enum DownloadStatus
    {
        Pending,
        Downloading,
        Completed,
        Failed
    }

    public enum DownloadType
    {
        Stock,
        Indice,
        Broad
    }

    public class DownloadItem : INotifyPropertyChanged
    {
        private string _sourceName = string.Empty;
        private DateTime _date;
        private DownloadStatus _status;
        private double _progress;
        private string _filePath = string.Empty;
        private DownloadType _type;

        public string SourceName
        {
            get => _sourceName;
            set
            {
                _sourceName = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public DateTime Date
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DateFormatted));
            }
        }

        public DownloadStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public double Progress
        {
            get => _progress;
            set
            {
                _progress = Math.Max(0, Math.Min(100, value)); // Clamp between 0 and 100
                OnPropertyChanged();
            }
        }

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public DownloadType Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public string DateFormatted => Date.ToString("yyyy-MM-dd");

        [JsonIgnore]
        public string StatusText => Status switch
        {
            DownloadStatus.Pending => "Pending",
            DownloadStatus.Downloading => "Downloading",
            DownloadStatus.Completed => "Completed",
            DownloadStatus.Failed => "Failed",
            _ => "Unknown"
        };

        [JsonIgnore]
        public string ProgressText => Status switch
        {
            DownloadStatus.Downloading => $"{Progress:F1}%",
            DownloadStatus.Completed => "100%",
            DownloadStatus.Failed => "Failed",
            _ => "Pending"
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Constructor for easier initialization
        public DownloadItem()
        {
            _sourceName = string.Empty;
            _filePath = string.Empty;
            _date = DateTime.Today;
            _status = DownloadStatus.Pending;
            _progress = 0;
            _type = DownloadType.Stock;
        }

        public DownloadItem(string sourceName, DateTime date, DownloadType type, string filePath)
        {
            _sourceName = sourceName;
            _date = date;
            _type = type;
            _filePath = filePath;
            _status = DownloadStatus.Pending;
            _progress = 0;
        }
    }
}