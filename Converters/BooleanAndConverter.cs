using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace LoadOrderKeeper.Converters
{
    public sealed class BooleanAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
            {
                return false;
            }

            return values.All(v => v is bool b && b);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack is not supported for BooleanAndConverter");
        }
    }
}
