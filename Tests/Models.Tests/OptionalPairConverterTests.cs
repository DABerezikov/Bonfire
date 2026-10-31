using System.Globalization;
using Bonfire.Infrastructure.Converters;

namespace Models.Tests;

public class OptionalPairConverterTests
{
    [Theory]
    [InlineData(null, null, null, "")]
    [InlineData(2, null, null, "от 2")]
    [InlineData(null, 3, null, "до 3")]
    [InlineData(2, 3, null, "2 - 3")]
    [InlineData(2, 3, "x", "2 x 3")]
    [InlineData(0, null, null, "от 0")]
    [InlineData(0, null, "x", "до 0")]
    [InlineData(null, 0, "x", "до 0")]
    public void Convert_OptionalBounds_FormatsPresentValues(int? first, int? second, string? separator, string expected)
    {
        var converter = new OptionalPairConverter();
        Assert.Equal(expected, converter.Convert([first!, second!], typeof(string), separator!, CultureInfo.GetCultureInfo("ru-RU")));
    }

    [Fact]
    public void Convert_FractionalBounds_UsesComma()
    {
        Assert.Equal("1,5 - 2,5", new OptionalPairConverter().Convert([1.5, 2.5], typeof(string), null!, CultureInfo.GetCultureInfo("ru-RU")));
    }
}
