using System;
using System.Globalization;
using System.Windows.Data;

namespace Bonfire.Infrastructure.Converters;

/// <summary>
/// Двусторонний конвертер пиксели ↔ метры (масштаб 40 пкс/м).
/// Convert: double пкс → string метры с шагом 0.05 м.
/// ConvertBack: string метры → double пкс (округлено до целого).
/// </summary>
[ValueConversion(typeof(double), typeof(string))]
public class PxToMetersConverter : IValueConverter
{
    private const double Scale = 40.0;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double px)
            return Math.Round(px / Scale, 2).ToString("G", culture);
        return "0";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (double.TryParse(value?.ToString(), NumberStyles.Any, culture, out var meters))
            return Math.Round(meters * Scale);
        return 0.0;
    }
}
