using Bonfire.Domain.Libraries;

namespace Models.Tests;

public class CultureDefinitionTests
{
    [Theory]
    [InlineData("", "Овощи")]
    [InlineData(" ", "Овощи")]
    [InlineData("Томат", "")]
    [InlineData("Томат", " ")]
    public void Create_EmptyNameOrClass_ReturnsFailure(string name, string plantClass)
        => Assert.True(CultureDefinition.Create(1, name, plantClass).IsFailure);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidIdentity_ReturnsFailure(int id)
        => Assert.True(CultureDefinition.Create(id, "Томат", "Овощи").IsFailure);

    [Fact]
    public void WithDetails_ValidChange_ReturnsNewCultureWithoutMutatingOriginal()
    {
        var original = CultureDefinition.Create(1, "Томат", "Овощи").ValueOrThrow();
        var updated = original.WithDetails("Астра", "Цветы").ValueOrThrow();
        Assert.NotSame(original, updated);
        Assert.Equal(original.Id, updated.Id);
        Assert.Equal("Томат", original.Name.Value);
        Assert.Equal("Овощи", original.Class);
        Assert.Equal("Астра", updated.Name.Value);
        Assert.Equal("Цветы", updated.Class);
    }

    [Fact]
    public void Create_NonemptyLegacyText_NormalizesNameAndPreservesCustomClass()
    {
        var culture = CultureDefinition.Create(1, " Томат ", "Особый класс").ValueOrThrow();
        Assert.Equal("Томат", culture.Name.Value);
        Assert.Equal("Особый класс", culture.Class);
    }

    [Fact]
    public void WithDetails_InvalidChange_ReturnsFailureAndPreservesOriginal()
    {
        var original = CultureDefinition.Create(1, "Томат", "Овощи").ValueOrThrow();
        Assert.True(original.WithDetails("", "Цветы").IsFailure);
        Assert.Equal("Томат", original.Name.Value);
    }
}
