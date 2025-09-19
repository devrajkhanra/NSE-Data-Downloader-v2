using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace NSE_Data_Downloader.Services
{
    public class FolderSetupService
    {
        private readonly Window _mainWindow;
        private XamlRoot _xamlRoot;

        public FolderSetupService(Window mainWindow)
        {
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        }

        /// <summary>
        /// Must be set after window is activated and content loaded.
        /// </summary>
        public void SetXamlRoot(XamlRoot xamlRoot)
        {
            _xamlRoot = xamlRoot ?? throw new ArgumentNullException(nameof(xamlRoot));
        }

        private readonly string defaultFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NSE-Data");

        private readonly string[] subFolders = { "broad", "stocks", "indices", "options", "ma" };

        public string CurrentFolderPath { get; private set; }

        public async Task InitializeAppFoldersAsync()
        {
            CurrentFolderPath = LoadFolderPathFromSettings();

            if (string.IsNullOrEmpty(CurrentFolderPath) || !Directory.Exists(CurrentFolderPath))
            {
                if (IsFirstLaunch())
                {
                    string selectedPath = await PromptForDefaultOrCustomPathAsync();

                    CurrentFolderPath = selectedPath ?? defaultFolder;

                    SaveFolderPathToSettings(CurrentFolderPath);
                    MarkUserFolderSelected();
                }
                else
                {
                    CurrentFolderPath = defaultFolder;
                }
            }

            Directory.CreateDirectory(CurrentFolderPath);
            foreach (var subFolder in subFolders)
            {
                Directory.CreateDirectory(Path.Combine(CurrentFolderPath, subFolder));
            }
        }

        private bool IsFirstLaunch()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("HasUserSelectedFolder"))
            {
                return !(bool)localSettings.Values["HasUserSelectedFolder"];
            }
            return true;
        }

        private void MarkUserFolderSelected()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["HasUserSelectedFolder"] = true;
        }

        private string LoadFolderPathFromSettings()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("NSEDataFolderPath"))
            {
                return localSettings.Values["NSEDataFolderPath"] as string;
            }
            return null;
        }

        private void SaveFolderPathToSettings(string path)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["NSEDataFolderPath"] = path;
        }

        private async Task<string> PromptForDefaultOrCustomPathAsync()
        {
            if (_xamlRoot == null)
                throw new InvalidOperationException("XamlRoot must be set before showing dialog.");

            var dialog = new ContentDialog()
            {
                Title = "Select NSE Data Folder",
                Content = "Do you want to use the default NSE-Data folder on your Desktop?",
                PrimaryButtonText = "Use Default",
                CloseButtonText = "Choose Folder",
                XamlRoot = _xamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                return null; // Use default path
            }
            else
            {
                return await PromptUserToSelectFolderAsync();
            }
        }

        public async Task<string> PromptUserToSelectFolderAsync()
        {
            var picker = new FolderPicker();
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(_mainWindow));
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");

            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                return folder.Path;
            }
            return null;
        }
    }
}
