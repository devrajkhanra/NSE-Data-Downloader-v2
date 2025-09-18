using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NSE_Data_Downloader.Models;
using NSE_Data_Downloader.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NSE_Data_Downloader.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private DateTime _startDate = DateTime.Today.AddDays(-7);
        private DateTime _endDate = DateTime.Today;
        private string _downloadFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "NSE-Data");
        private bool _downloadStock = true;
        private bool _downloadIndices = true;
        private bool _downloadBroad = false;
        private bool _isDownloading = false;

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    OnPropertyChanged(nameof(CanStartDownload));
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    OnPropertyChanged(nameof(CanStartDownload));
                }
            }
        }

        public string DownloadFolder
        {
            get => _downloadFolder;
            set => SetProperty(ref _downloadFolder, value);
        }

        public bool DownloadStock
        {
            get => _downloadStock;
            set
            {
                if (SetProperty(ref _downloadStock, value))
                {
                    OnPropertyChanged(nameof(CanStartDownload));
                }
            }
        }

        public bool DownloadIndices
        {
            get => _downloadIndices;
            set
            {
                if (SetProperty(ref _downloadIndices, value))
                {
                    OnPropertyChanged(nameof(CanStartDownload));
                }
            }
        }

        public bool DownloadBroad
        {
            get => _downloadBroad;
            set
            {
                if (SetProperty(ref _downloadBroad, value))
                {
                    OnPropertyChanged(nameof(CanStartDownload));
                }
            }
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                if (SetProperty(ref _isDownloading, value))
                {
                    OnPropertyChanged(nameof(CanStartDownload));
                }
            }
        }

        public ObservableCollection<DownloadItem> Downloads { get; } = new ObservableCollection<DownloadItem>();

        private readonly DownloaderService _downloader = new();
        private readonly SemaphoreSlim _semaphore = new(3);
        private CancellationTokenSource _cancellationTokenSource = new();

        private readonly string _historyFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NSEDataDownloader",
            "downloads_history.json");

        public bool HasDownloads => Downloads.Any();
        public int TotalCount => Downloads.Count;
        public int CompletedCount => Downloads.Count(d => d.Status == DownloadStatus.Completed);
        public int FailedCount => Downloads.Count(d => d.Status == DownloadStatus.Failed);
        public int PendingCount => Downloads.Count(d => d.Status == DownloadStatus.Pending);
        public int DownloadingCount => Downloads.Count(d => d.Status == DownloadStatus.Downloading);

        public string OverallProgressText
        {
            get
            {
                if (TotalCount == 0) return "";
                var percentage = (double)CompletedCount / TotalCount * 100;
                return $"{percentage:F1}% complete";
            }
        }

        public bool CanStartDownload => !IsDownloading && (DownloadStock || DownloadIndices || DownloadBroad) && StartDate <= EndDate;

        public MainViewModel()
        {
            Downloads.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(HasDownloads));
                OnPropertyChanged(nameof(TotalCount));
                OnPropertyChanged(nameof(CompletedCount));
                OnPropertyChanged(nameof(FailedCount));
                OnPropertyChanged(nameof(PendingCount));
                OnPropertyChanged(nameof(DownloadingCount));
                OnPropertyChanged(nameof(OverallProgressText));
            };

            _ = LoadHistoryAsync();
        }

        partial void OnDownloadStockChanged(bool value) => OnPropertyChanged(nameof(CanStartDownload));
        partial void OnDownloadIndicesChanged(bool value) => OnPropertyChanged(nameof(CanStartDownload));
        partial void OnDownloadBroadChanged(bool value) => OnPropertyChanged(nameof(CanStartDownload));
        partial void OnStartDateChanged(DateTime value) => OnPropertyChanged(nameof(CanStartDownload));
        partial void OnEndDateChanged(DateTime value) => OnPropertyChanged(nameof(CanStartDownload));
        partial void OnIsDownloadingChanged(bool value) => OnPropertyChanged(nameof(CanStartDownload));

        [RelayCommand]
        private async Task StartDownloadsAsync()
        {
            if (IsDownloading) return;

            IsDownloading = true;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                var tasks = new List<Task>();
                var downloadTypes = new List<DownloadType>();

                if (DownloadStock) downloadTypes.Add(DownloadType.Stock);
                if (DownloadIndices) downloadTypes.Add(DownloadType.Indice);
                if (DownloadBroad) downloadTypes.Add(DownloadType.Broad);

                for (var date = StartDate; date <= EndDate; date = date.AddDays(1))
                {
                    // Skip weekends for stock market data
                    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                        continue;

                    foreach (var type in downloadTypes)
                    {
                        // Broad market data doesn't need daily downloads
                        if (type == DownloadType.Broad && date != StartDate)
                            continue;

                        var item = new DownloadItem
                        {
                            SourceName = GetSourceName(type),
                            Date = date,
                            Status = DownloadStatus.Pending,
                            FilePath = GetFilePath(type, date),
                            Progress = 0,
                            Type = type
                        };

                        Downloads.Add(item);

                        tasks.Add(DownloadWithSemaphoreAsync(item, _cancellationTokenSource.Token));
                    }
                }

                await Task.WhenAll(tasks);
            }
            finally
            {
                IsDownloading = false;
            }
        }

        [RelayCommand]
        private void ClearDownloads()
        {
            Downloads.Clear();
            _ = SaveHistoryAsync();
        }

        private async Task DownloadWithSemaphoreAsync(DownloadItem item, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await _downloader.DownloadAsync(item);

                // Update UI properties
                OnPropertyChanged(nameof(CompletedCount));
                OnPropertyChanged(nameof(FailedCount));
                OnPropertyChanged(nameof(OverallProgressText));
            }
            catch (OperationCanceledException)
            {
                item.Status = DownloadStatus.Failed;
            }
            finally
            {
                _semaphore.Release();
                await SaveHistoryAsync();
            }
        }

        public async Task RetryDownloadAsync(DownloadItem item)
        {
            if (item.Status != DownloadStatus.Failed) return;

            item.Status = DownloadStatus.Pending;
            item.Progress = 0;

            await DownloadWithSemaphoreAsync(item, CancellationToken.None);
        }

        private string GetSourceName(DownloadType type)
        {
            return type switch
            {
                DownloadType.Stock => "Stock Data",
                DownloadType.Indice => "Indices",
                DownloadType.Broad => "Broad Market",
                _ => "Unknown"
            };
        }

        private string GetFilePath(DownloadType type, DateTime date)
        {
            var folderName = type.ToString();
            var fileName = type switch
            {
                DownloadType.Stock => $"sec_bhavdata_full_{date:ddMMyyyy}.csv",
                DownloadType.Indice => $"ind_close_all_{date:ddMMyyyy}.csv",
                DownloadType.Broad => "ind_nifty50list.csv",
                _ => throw new InvalidOperationException("Unknown type")
            };

            return Path.Combine(DownloadFolder, folderName, fileName);
        }

        public async Task LoadHistoryAsync()
        {
            try
            {
                if (File.Exists(_historyFile))
                {
                    var json = await File.ReadAllTextAsync(_historyFile);
                    if (!string.IsNullOrEmpty(json))
                    {
                        var items = JsonSerializer.Deserialize<List<DownloadItem>>(json);

                        if (items != null)
                        {
                            Downloads.Clear();
                            foreach (var item in items)
                            {
                                Downloads.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load history: {ex.Message}");
            }
        }

        public async Task SaveHistoryAsync()
        {
            try
            {
                var directory = Path.GetDirectoryName(_historyFile);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(Downloads.ToList(), options);
                await File.WriteAllTextAsync(_historyFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save history: {ex.Message}");
            }
        }
    }
}