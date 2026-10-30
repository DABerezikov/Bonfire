using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace Bonfire.Infrastructure.Converters;

/// <summary>
/// Двусторонний конвертер пиксели ↔ метры (масштаб CanvasConstants.PixelsPerMeter пкс/м).
/// Convert: double пкс → string метры.
/// ConvertBack: string метры → double пкс (округлено до целого).
/// </summary>
[ValueConversion(typeof(double), typeof(string))]
public class PxToMetersConverter : IValueConverter
{
    public bool RejectInvalidInput { get; set; }
    private static readonly double Scale = CanvasConstants.PixelsPerMeter;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double px)
            return Math.Round(px / Scale, 2).ToString("G", culture);
        return "0";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (double.TryParse(value?.ToString(), NumberStyles.Any, culture, out var meters))
        {
            var pixels = Math.Round(meters * Scale);
            if (!RejectInvalidInput || double.IsFinite(pixels)) return pixels;
        }
        return RejectInvalidInput ? DependencyProperty.UnsetValue : 0.0;
    }
}
