namespace Services.Tests;

public class StringExtensionTests
{
    // ── DoubleParseAdvanced ───────────────────────────────────────────────────

    [Fact]
    public void DoubleParseAdvanced_WholeNumber_Parses()
    {
        Assert.Equal(42.0, "42".DoubleParseAdvanced());
    }

    [Fact]
    public void DoubleParseAdvanced_CommaDecimal_Parses()
    {
        var result = "3,14".DoubleParseAdvanced(',');
        Assert.Equal(3.14, result, 2);
    }

    [Fact]
    public void DoubleParseAdvanced_DotDecimal_Parses()
    {
        var result = "3.14".DoubleParseAdvanced('.');
        Assert.Equal(3.14, result, 2);
    }

    [Fact]
    public void DoubleParseAdvanced_NegativeNumber_Parses()
    {
        Assert.Equal(-5.0, "-5".DoubleParseAdvanced());
    }

    [Fact]
    public void DoubleParseAdvanced_EmptyString_ReturnsZero()
    {
        Assert.Equal(0.0, "".DoubleParseAdvanced());
    }

    [Fact]
    public void DoubleParseAdvanced_NonNumericString_ReturnsZero()
    {
        Assert.Equal(0.0, "abc".DoubleParseAdvanced());
    }

    [Fact]
    public void DoubleParseAdvanced_NumberWithText_ExtractsNumber()
    {
        var result = "100 шт".DoubleParseAdvanced();
        Assert.Equal(100.0, result);
    }

    // ── DecimalParseAdvanced ──────────────────────────────────────────────────

    [Fact]
    public void DecimalParseAdvanced_WholeNumber_Parses()
    {
        Assert.Equal(99m, "99".DecimalParseAdvanced());
    }

    [Fact]
    public void DecimalParseAdvanced_CommaDecimal_Parses()
    {
        var result = "12,50".DecimalParseAdvanced(',');
        Assert.Equal(12.50m, result);
    }

    [Fact]
    public void DecimalParseAdvanced_EmptyString_ReturnsZero()
    {
        Assert.Equal(0m, "".DecimalParseAdvanced());
    }

    [Fact]
    public void DecimalParseAdvanced_NonNumericString_ReturnsZero()
    {
        Assert.Equal(0m, "xyz".DecimalParseAdvanced());
    }
}
