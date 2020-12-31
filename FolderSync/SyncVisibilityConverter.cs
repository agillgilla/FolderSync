using System;
using System.Windows;
using System.Windows.Data;

namespace FolderSync
{
    class SyncVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value == null)
                return Visibility.Visible;


            SyncState syncState = (SyncState)value;

            return (MainWindow.HideSyncedFiles && syncState == SyncState.Synced) ? Visibility.Hidden : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
