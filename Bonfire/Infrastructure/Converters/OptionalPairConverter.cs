using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Bonfire.Infrastructure.Converters;

public sealed class OptionalPairConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2 || Array.Exists(values, value => value == DependencyProperty.UnsetValue))
            return string.Empty;

        string Format(object value) => System.Convert.ToString(value, culture) ?? string.Empty;
        return (values[0], values[1]) switch
        {
            (null, null) => string.Empty,
            (null, var second) => $"до {Format(second)}",
            (var first, null) when parameter as string == "x" => $"до {Format(first)}",
            (var first, null) => $"от {Format(first)}",
            (var first, var second) => $"{Format(first)} {parameter as string ?? "-"} {Format(second)}"
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
