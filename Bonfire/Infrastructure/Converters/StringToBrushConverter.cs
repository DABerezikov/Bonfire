using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Bonfire.Infrastructure.Converters;

/// <summary>Конвертирует hex-строку цвета в SolidColorBrush.</summary>
[ValueConversion(typeof(string), typeof(Brush))]
public class StringToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
        catch { /* некорректный цвет — прозрачный */ }
        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
