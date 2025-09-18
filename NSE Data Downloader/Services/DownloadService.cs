using NSE_Data_Downloader.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NSE_Data_Downloader.Services
{
    public class DownloaderService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private bool _disposed = false;

        public DownloaderService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);

            // Set user agent to avoid potential blocking
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        public async Task DownloadAsync(DownloadItem item, CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DownloaderService));

            try
            {
                item.Status = DownloadStatus.Downloading;
                item.Progress = 0;

                string url = GetDownloadUrl(item);
                string filePath = GetFullFilePath(item);

                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Check if file already exists and is not empty
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length > 0)
                    {
                        item.Status = DownloadStatus.Completed;
                        item.Progress = 100;
                        item.FilePath = filePath;
                        return;
                    }
                }

                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // Check if the response is successful
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"HTTP {response.StatusCode}: {response.ReasonPhrase}");
                }

                var totalBytes = response.Content.Headers.ContentLength ?? 0;

                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);

                var buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    totalRead += bytesRead;

                    // Update progress
                    if (totalBytes > 0)
                    {
                        item.Progress = (double)totalRead / totalBytes * 100;
                    }
                    else
                    {
                        // If content length is unknown, show indeterminate progress
                        item.Progress = Math.Min(50 + (totalRead / 1024.0 / 1024.0 * 10), 90); // Rough estimate
                    }
                }

                // Verify file was created and has content
                var downloadedFileInfo = new FileInfo(filePath);
                if (!downloadedFileInfo.Exists || downloadedFileInfo.Length == 0)
                {
                    throw new InvalidDataException("Downloaded file is empty or was not created");
                }

                item.FilePath = filePath;
                item.Progress = 100;
                item.Status = DownloadStatus.Completed;
            }
            catch (OperationCanceledException)
            {
                item.Status = DownloadStatus.Failed;
                item.Progress = 0;
                throw; // Re-throw to handle cancellation properly
            }
            catch (Exception ex)
            {
                item.Status = DownloadStatus.Failed;
                item.Progress = 0;

                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Download failed for {item.SourceName} on {item.Date:yyyy-MM-dd}: {ex.Message}");

                // Clean up partial file
                try
                {
                    if (!string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
                    {
                        File.Delete(item.FilePath);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        private string GetDownloadUrl(DownloadItem item)
        {
            return item.Type switch
            {
                DownloadType.Stock => $"https://archives.nseindia.com/products/content/sec_bhavdata_full_{item.Date:ddMMyyyy}.csv",
                DownloadType.Indice => $"https://archives.nseindia.com/content/indices/ind_close_all_{item.Date:ddMMyyyy}.csv",
                DownloadType.Broad => "https://archives.nseindia.com/content/indices/ind_nifty50list.csv",
                _ => throw new InvalidOperationException($"Unknown download type: {item.Type}")
            };
        }

        private string GetFullFilePath(DownloadItem item)
        {
            var baseDirectory = Path.GetDirectoryName(item.FilePath);
            if (string.IsNullOrEmpty(baseDirectory))
                throw new InvalidOperationException("Invalid file path");

            var folderName = item.Type.ToString();
            var targetFolder = Path.Combine(baseDirectory, folderName);

            var fileName = item.Type switch
            {
                DownloadType.Stock => $"sec_bhavdata_full_{item.Date:ddMMyyyy}.csv",
                DownloadType.Indice => $"ind_close_all_{item.Date:ddMMyyyy}.csv",
                DownloadType.Broad => "ind_nifty50list.csv",
                _ => throw new InvalidOperationException($"Unknown download type: {item.Type}")
            };

            return Path.Combine(targetFolder, fileName);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}