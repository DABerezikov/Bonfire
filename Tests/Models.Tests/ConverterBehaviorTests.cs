using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Bonfire.Infrastructure.Converters;
using Bonfire.Infrastructure;

namespace Models.Tests;

public class ConverterBehaviorTests
{
    [Theory]
    [InlineData("ru-RU", "1,5")]
    [InlineData("en-US", "1.5")]
    public void PixelsAndMeters_RoundTripUsesRequestedCulture(string cultureName, string text)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var converter = new PxToMetersConverter();
        Assert.Equal(text, converter.Convert(1.5 * CanvasConstants.PixelsPerMeter, typeof(string), null, culture));
        Assert.Equal(Math.Round(1.5 * CanvasConstants.PixelsPerMeter), converter.ConvertBack(text, typeof(double), null, culture));
        Assert.Equal("0", converter.Convert(null, typeof(string), null, culture));
        Assert.Equal(0d, converter.ConvertBack("не число", typeof(double), null, culture));
        Assert.Equal(0d, converter.ConvertBack(null, typeof(double), null, culture));
    }

    [Theory]
    [InlineData(0.00667, "0.01")]
    [InlineData(1, "1")]
    [InlineData(2.00667, "2.01")]
    public void PixelsToMeters_RoundsForDisplay(double pixels, string expected)
        => Assert.Equal(expected, new PxToMetersConverter().Convert(pixels * CanvasConstants.PixelsPerMeter, typeof(string), null, CultureInfo.InvariantCulture));

    [Theory]
    [InlineData(null, true)]
    [InlineData("", false)]
    [InlineData("значение", false)]
    public void Visibility_ConvertersHandleNullIndependentlyOfEmptyString(string? value, bool isNull)
    {
        var culture = CultureInfo.InvariantCulture;
        Assert.Equal(isNull ? Visibility.Collapsed : Visibility.Visible, new NullToVisibilityConverter().Convert(value, typeof(Visibility), null, culture));
        Assert.Equal(isNull ? Visibility.Visible : Visibility.Collapsed, new NotNullToVisibilityConverter().Convert(value, typeof(Visibility), null, culture));
    }

    [Theory]
    [InlineData(true, Visibility.Collapsed)]
    [InlineData(false, Visibility.Visible)]
    [InlineData(null, Visibility.Visible)]
    public void InverseBooleanVisibility_UsesBooleanValue(object? value, Visibility expected)
        => Assert.Equal(expected, new NotBoolToVisibilityConverter().Convert(value, typeof(Visibility), null, CultureInfo.InvariantCulture));

    [Theory]
    [InlineData("#FF0000", 255, 0, 0)]
    [InlineData("#008000", 0, 128, 0)]
    [InlineData("Blue", 0, 0, 255)]
    public void StringToBrush_ParsesValidColors(string text, byte red, byte green, byte blue)
    {
        var brush = Assert.IsType<SolidColorBrush>(new StringToBrushConverter().Convert(text, typeof(Brush), null, CultureInfo.InvariantCulture));
        Assert.Equal(Color.FromRgb(red, green, blue), brush.Color);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-a-color")]
    public void StringToBrush_InvalidInput_IsTransparent(string? text)
        => Assert.Same(Brushes.Transparent, new StringToBrushConverter().Convert(text, typeof(Brush), null, CultureInfo.InvariantCulture));

    [Fact]
    public void OneWayConverters_RejectReverseConversion()
    {
        var culture = CultureInfo.InvariantCulture;
        Assert.Throws<NotSupportedException>(() => new NullToVisibilityConverter().ConvertBack(null, typeof(object), null, culture));
        Assert.Throws<NotSupportedException>(() => new NotNullToVisibilityConverter().ConvertBack(null, typeof(object), null, culture));
        Assert.Throws<NotSupportedException>(() => new NotBoolToVisibilityConverter().ConvertBack(null, typeof(object), null, culture));
        Assert.Throws<NotSupportedException>(() => new StringToBrushConverter().ConvertBack(null, typeof(string), null, culture));
    }
}
