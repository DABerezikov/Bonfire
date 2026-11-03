using Bonfire.Domain.Libraries;

namespace Models.Tests;

public class LibraryNameTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\r\n")]
    public void Create_MissingText_ReturnsFailure(string? value)
        => Assert.True(LibraryName.Create(value!).IsFailure);

    [Fact]
    public void Create_NonemptyText_NormalizesWhitespaceAndPreservesValueEquality()
    {
        var name = LibraryName.Create(" Фирма ").ValueOrThrow();
        Assert.Equal("Фирма", name.Value);
        Assert.Equal(name, LibraryName.Create(" Фирма ").ValueOrThrow());
    }
    [Theory]
    [InlineData("  Розовая  фантазия ", "Розовая фантазия")]
    [InlineData("Розовая\u00a0фантазия\t", "Розовая фантазия")]
    [InlineData(" МАЛОПА ", "МАЛОПА")]
    public void Create_WhitespaceVariants_ProducesCanonicalName(string input, string expected)
        => Assert.Equal(expected, LibraryName.Create(input).ValueOrThrow().Value);
}
