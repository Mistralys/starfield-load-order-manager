using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LoadOrderKeeper.Converters;

/// <summary>
/// Converts a profile id and the active profile id into a visibility state.
/// </summary>
public sealed class ActiveProfileVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return Visibility.Collapsed;
        }

        string? profileId = values[0]?.ToString();
        string? activeProfileId = values[1]?.ToString();
        bool isActive = !string.IsNullOrWhiteSpace(profileId) &&
                        !string.IsNullOrWhiteSpace(activeProfileId) &&
                        string.Equals(profileId, activeProfileId, StringComparison.OrdinalIgnoreCase);

        return isActive ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
