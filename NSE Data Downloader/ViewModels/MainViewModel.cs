using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NSE_Data_Downloader.Services;
using System.Threading.Tasks;

namespace NSE_Data_Downloader.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly FolderSetupService _folderService;

        [ObservableProperty]
        private string statusMessage;

        public MainViewModel(FolderSetupService folderService)
        {
            _folderService = folderService;
            StatusMessage = "Initializing...";
            // Remove direct call to InitializeAsync() here; call it explicitly after construction
        }

        [RelayCommand]
        private async Task SetFolderAsync()
        {
            await _folderService.InitializeAppFoldersAsync();
            StatusMessage = $"NSE-Data folder is set at {_folderService.CurrentFolderPath}.";
        }

        // Make InitializeAsync public and returning Task so it can be awaited
        public async Task InitializeAsync()
        {
            await _folderService.InitializeAppFoldersAsync();
            StatusMessage = $"NSE-Data folder checked at {_folderService.CurrentFolderPath}.";
        }
    }
}
