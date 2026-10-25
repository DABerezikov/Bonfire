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
    public void Create_NonemptyText_PreservesWhitespaceAndValueEquality()
    {
        var name = LibraryName.Create(" Фирма ").ValueOrThrow();
        Assert.Equal(" Фирма ", name.Value);
        Assert.Equal(name, LibraryName.Create(" Фирма ").ValueOrThrow());
    }
}
