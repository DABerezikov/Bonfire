using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Bonfire.Infrastructure.Converters;

/// <summary>
///     Двусторонний конвертер пиксели ↔ метры (масштаб CanvasConstants.PixelsPerMeter пкс/м).
///     Convert: double пкс → string метры.
///     ConvertBack: string метры → double пкс (округлено до целого).
/// </summary>
[ValueConversion(typeof(double), typeof(string))]
public class PxToMetersConverter : IValueConverter
{
    private static readonly double Scale = CanvasConstants.PixelsPerMeter;
    public bool RejectInvalidInput { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double px
            ? Math.Round(px / Scale, 2).ToString("G", culture)
            : "0";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!double.TryParse(value?.ToString(), NumberStyles.Any, culture, out var meters))
            return RejectInvalidInput ? DependencyProperty.UnsetValue : 0.0;

        var pixels = Math.Round(meters * Scale);

        return RejectInvalidInput && !double.IsFinite(pixels)
            ? RejectInvalidInput
                ? DependencyProperty.UnsetValue
                : 0.0
            : pixels;
    }
}