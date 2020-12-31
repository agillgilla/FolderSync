using System;
using System.Windows.Data;
using System.Windows.Media;

namespace FolderSync
{
    class EntryColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value == null)
                return Brushes.Black;


            SyncState syncState = (SyncState)value;

            switch (syncState) {
                case SyncState.Synced:
                    return Brushes.Black;
                case SyncState.New:
                    return Brushes.Green;
                case SyncState.Modified:
                    return Brushes.Blue;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
