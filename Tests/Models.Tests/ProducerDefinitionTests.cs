using Bonfire.Domain.Libraries;

namespace Models.Tests;

public class ProducerDefinitionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidIdentity_ReturnsFailure(int id)
        => Assert.True(ProducerDefinition.Create(id, "Фирма").IsFailure);

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_MissingName_ReturnsFailure(string name)
        => Assert.True(ProducerDefinition.Create(1, name).IsFailure);

    [Fact]
    public void WithName_ValidName_ReturnsNewProducerWithoutChangingOriginal()
    {
        var original = ProducerDefinition.Create(1, "Фирма").ValueOrThrow();
        var updated = original.WithName(" Другое имя ").ValueOrThrow();
        Assert.NotSame(original, updated);
        Assert.Equal(original.Id, updated.Id);
        Assert.Equal("Фирма", original.Name.Value);
        Assert.Equal(" Другое имя ", updated.Name.Value);
    }

    [Fact]
    public void WithName_InvalidName_ReturnsFailureAndPreservesOriginal()
    {
        var original = ProducerDefinition.Create(1, "Фирма").ValueOrThrow();
        Assert.True(original.WithName("").IsFailure);
        Assert.Equal("Фирма", original.Name.Value);
    }
}
