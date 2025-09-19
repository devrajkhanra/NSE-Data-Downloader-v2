using Microsoft.UI.Xaml;
using NSE_Data_Downloader.ViewModels;

namespace NSE_Data_Downloader
{
    public partial class MainWindow : Window
    {
        public Microsoft.UI.Xaml.Controls.Grid RootGridPublic => this.RootGrid;

        public MainWindow()
        {
            InitializeComponent();
        }
    }
}