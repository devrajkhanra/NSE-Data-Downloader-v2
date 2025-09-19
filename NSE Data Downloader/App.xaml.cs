using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using NSE_Data_Downloader.Services;
using NSE_Data_Downloader.ViewModels;
using System;
using Windows.ApplicationModel.Activation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NSE_Data_Downloader
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {

        private Window _mainWindow;
        public IServiceProvider Services { get; }

        public App()
        {
            InitializeComponent();
            Services = ConfigureServices();
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<FolderSetupService>();
            services.AddTransient<MainViewModel>();
            return services.BuildServiceProvider();
        }
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _mainWindow = new MainWindow();

            _mainWindow.Activate();

            // Subscribe to Loaded event on the content to wait for XamlRoot readiness
            if (_mainWindow.Content is FrameworkElement content)
            {
                content.Loaded += MainWindowContent_Loaded;
            }
        }

        private async void MainWindowContent_Loaded(object sender, RoutedEventArgs e)
        {
            var content = sender as FrameworkElement;

            // Detach handler after loading
            content.Loaded -= MainWindowContent_Loaded;

            var xamlRoot = content.XamlRoot;

            var folderSetupService = new FolderSetupService(_mainWindow);
            folderSetupService.SetXamlRoot(xamlRoot);

            var mainViewModel = new MainViewModel(folderSetupService);

            (_mainWindow as MainWindow).RootGridPublic.DataContext = mainViewModel;

            // Initialize folders here safely after XamlRoot ready
            await mainViewModel.InitializeAsync(); // Make sure you have this method async in VM
        }

    }
}
