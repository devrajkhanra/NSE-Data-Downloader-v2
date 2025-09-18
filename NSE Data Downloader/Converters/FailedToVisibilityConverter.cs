using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using NSE_Data_Downloader.Models;

namespace NSE_Data_Downloader.Converters
{
    public class FailedToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is DownloadStatus status && status == DownloadStatus.Failed
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DownloadStatus status)
            {
                return status switch
                {
                    DownloadStatus.Pending => new SolidColorBrush(Colors.Gray),
                    DownloadStatus.Downloading => new SolidColorBrush(Colors.DodgerBlue),
                    DownloadStatus.Completed => new SolidColorBrush(Colors.Green),
                    DownloadStatus.Failed => new SolidColorBrush(Colors.Crimson),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool boolValue && boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Visibility.Visible;
        }
    }

    public class InvertBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool boolValue && !boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Visibility.Collapsed;
        }
    }
}