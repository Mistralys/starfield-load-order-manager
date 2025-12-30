using System;
using System.Globalization;
using System.Windows.Data;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Converters
{
    public sealed class ReplacementCommandParameterConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Length < 2)
            {
                return null;
            }

            if (values[0] is DiffLineModel replacement && values[1] is DiffLineModel removed)
            {
                return (removed, replacement);
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
