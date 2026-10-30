using Bonfire.Domain.Functional;

namespace Models.Tests;

public class OptionConversionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(0d)]
    [InlineData(2.5)]
    [InlineData(double.NaN)]
    public void NullableValue_ToOptionAndBack_PreservesAbsenceAndValue(double? value)
    {
        var option = value.ToOption();
        if (value is { } present)
            Assert.Equal(present, Assert.IsType<Option<double>.Some>(option).Value);
        else
            Assert.IsType<Option<double>.None>(option);
        Assert.Equal(value, option.ToNullable());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Текст")]
    public void NullableString_ToOptionAndBack_PreservesNullEmptyAndWhitespace(string? value)
    {
        var option = value.ToOption();
        if (value is { } present)
            Assert.Equal(present, Assert.IsType<Option<string>.Some>(option).Value);
        else
            Assert.IsType<Option<string>.None>(option);
        Assert.Equal(value, option.ToNullable());
    }

    [Fact]
    public void NullableConversions_OtherStructs_PreserveTypesAndZero()
    {
        Assert.Equal(0, ((int?)0).ToOption().ToNullable());
        Assert.Equal(12.34m, ((decimal?)12.34m).ToOption().ToNullable());
        var date = new DateTime(2026, 10, 25);
        Assert.Equal(date, Option.Some(date).ToNullable());
        Assert.Null(Option.None<int>().ToNullable());
    }

    [Fact]
    public void ToNullable_NullOption_ThrowsInvariantException()
    {
        Assert.Throws<ArgumentNullException>(() => ((Option<int>)null!).ToNullable());
        Assert.Throws<ArgumentNullException>(() => ((Option<string>)null!).ToNullable());
    }
}
