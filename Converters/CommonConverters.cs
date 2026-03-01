using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TonysDbTools.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNull = value == null;
        if (parameter != null && parameter.ToString() == "Inverse")
        {
            return isNull ? Visibility.Visible : Visibility.Collapsed;
        }
        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool b) return Visibility.Collapsed;

        bool invert = parameter != null && parameter.ToString() == "Inverse";
        if (invert) b = !b;

        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
