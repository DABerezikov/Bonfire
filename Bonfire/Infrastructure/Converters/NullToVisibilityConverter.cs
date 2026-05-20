using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Bonfire.Infrastructure.Converters;

[ValueConversion(typeof(object), typeof(Visibility))]
public class NullToVisibilityConverter : IValueConverter
{
    // null → Collapsed (объект отсутствует — скрыть)
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

[ValueConversion(typeof(object), typeof(Visibility))]
public class NotNullToVisibilityConverter : IValueConverter
{
    // not null → Collapsed (объект есть — скрыть «заглушку»)
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public class NotBoolToVisibilityConverter : IValueConverter
{
    // false → Visible, true → Collapsed (инверсия BooleanToVisibilityConverter)
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
