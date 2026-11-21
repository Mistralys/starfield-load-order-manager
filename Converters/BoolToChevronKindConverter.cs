using System;
using System.Globalization;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;

namespace LoadOrderKeeper.Converters
{
    /// <summary>
    /// Converts a <see cref="bool"/> expansion state to a <see cref="PackIconKind"/> chevron.
    /// <c>true</c> (expanded) returns <see cref="PackIconKind.ChevronDown"/>;
    /// <c>false</c> (collapsed) returns <see cref="PackIconKind.ChevronRight"/>.
    /// </summary>
    public class BoolToChevronKindConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? PackIconKind.ChevronDown : PackIconKind.ChevronRight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
