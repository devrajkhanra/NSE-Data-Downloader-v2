using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NSE_Data_Downloader.Models;
using NSE_Data_Downloader.ViewModels;
using System;
using System.ComponentModel;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace NSE_Data_Downloader
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            this.InitializeComponent();
            ViewModel = new MainViewModel();

            // Set minimum window size for better UX
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1000, 700));
        }

        private async void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();

            // Initialize with window handle for WinUI 3
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));

            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");

            try
            {
                StorageFolder? folder = await picker.PickSingleFolderAsync();

                if (folder != null)
                {
                    ViewModel.DownloadFolder = folder.Path;
                }
            }
            catch (System.Exception ex)
            {
                // Handle picker errors gracefully
                System.Diagnostics.Debug.WriteLine($"Folder picker error: {ex.Message}");

                // Show a simple content dialog for error feedback
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "Unable to access folder picker. Please try again.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };

                await dialog.ShowAsync();
            }
        }

        private async void RetryDownload_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DownloadItem item)
            {
                await ViewModel.RetryDownloadAsync(item);
            }
        }
    }
}
}