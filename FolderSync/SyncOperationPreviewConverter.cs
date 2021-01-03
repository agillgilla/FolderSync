using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FolderSync
{
    class SyncOperationPreviewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value == null)
                return "";


            SyncOperation syncOperation = (SyncOperation)value;

            return string.Format("{0}\t\t\t\t\t\t--> {1}", syncOperation.SourceFilepath, syncOperation.DestFilepath);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
