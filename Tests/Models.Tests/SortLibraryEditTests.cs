using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Models.Tests;

public class SortLibraryEditTests
{
    [Theory]
    [InlineData(0, "Имя")]
    [InlineData(-1, "Имя")]
    [InlineData(1, null)]
    [InlineData(1, "")]
    [InlineData(1, " ")]
    public void Create_InvalidIdentityOrName_ReturnsFailure(int id, string? name)
        => Assert.True(SortLibraryEdit.Create(id, name!, Option.None<int>()).IsFailure);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidCulture_ReturnsFailure(int culture)
        => Assert.True(SortLibraryEdit.Create(1, "Сорт", Option.Some(culture)).IsFailure);

    [Fact]
    public void Create_NoCulture_TrimsNameAndExplicitlyPreservesCultureLinks()
    {
        var edit = SortLibraryEdit.Create(1, " Сорт ", Option.None<int>()).ValueOrThrow();
        Assert.Equal(1, edit.Id.Value);
        Assert.Equal("Сорт", edit.Name.Value);
        Assert.IsType<Option<CultureId>.None>(edit.CultureChange);
    }

    [Fact]
    public void WithDetails_ValidChange_ReturnsNewEditWithoutChangingOriginal()
    {
        var original = SortLibraryEdit.Create(1, "Сорт", Option.None<int>()).ValueOrThrow();
        var updated = original.WithDetails(" Новый ", Option.Some(2)).ValueOrThrow();
        Assert.NotSame(original, updated);
        Assert.Equal(original.Id, updated.Id);
        Assert.Equal("Сорт", original.Name.Value);
        Assert.IsType<Option<CultureId>.None>(original.CultureChange);
        Assert.Equal("Новый", updated.Name.Value);
        Assert.Equal(2, Assert.IsType<Option<CultureId>.Some>(updated.CultureChange).Value.Value);
    }

    [Fact]
    public void WithDetails_InvalidChange_PreservesOriginal()
    {
        var original = SortLibraryEdit.Create(1, "Сорт", Option.Some(2)).ValueOrThrow();
        Assert.True(original.WithDetails("", Option.None<int>()).IsFailure);
        Assert.Equal("Сорт", original.Name.Value);
        Assert.Equal(2, Assert.IsType<Option<CultureId>.Some>(original.CultureChange).Value.Value);
    }
}
