using System;
using System.Globalization;
using System.Windows.Data;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Converters
{
    public sealed class ChangeSummaryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ReferenceVersionMetadataModel metadata)
            {
                return metadata.GetChangeSummary();
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
