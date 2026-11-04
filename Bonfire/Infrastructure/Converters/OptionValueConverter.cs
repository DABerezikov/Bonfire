using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Bonfire.Domain.Functional;

namespace Bonfire.Infrastructure.Converters;

// Nullable появляется только на границе привязки WPF; Some(0)/Some("") сохраняются.
public sealed class OptionValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        Option<string> text => text.ToNullable(),
        Option<int> number => number.ToNullable(),
        Option<double> number => number.ToNullable(),
        null => null,
        _ => DependencyProperty.UnsetValue
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}