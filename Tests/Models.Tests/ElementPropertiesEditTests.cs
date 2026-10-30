using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Models.Tests;

public class ElementPropertiesEditTests
{
    private static ElementDetailsValues Details(string name = "Грядка") => new(name, Option.Some("Заметка"), Option.None<string>(), Option.Some("Север"), Option.None<string>());
    private static ElementEditorValues Original() => new(Details(), 0, 0, 90, 90);
    private static ElementPropertiesState State() => new(1, Details(), CanvasRectangle.Create(0, 0, 90, 90).ValueOrThrow(), 0.4, false);
    private static ElementPropertiesEdit Edit(ElementEditorValues values) => ElementPropertiesEdit.Create(1, Original(), values, 150).ValueOrThrow();

    [Fact]
    public void ApplyTo_MetadataOnly_PreservesFreshGeometryAreaLockAndUntouchedProperties()
    {
        var current = State() with { Rectangle = CanvasRectangle.Create(30, 45, 180, 90).ValueOrThrow(), AreaSquareMeters = 0.7,
            IsLocked = true, Details = Details() with { Note = Option.Some("Свежая заметка") } };
        var changed = Edit(Original() with { Details = Details("Новое") }).ApplyTo(current, new PlotLayout([])).ValueOrThrow();
        Assert.Equal("Новое", changed.Details.Name);
        Assert.Equal("Свежая заметка", changed.Details.Note.ToNullable());
        Assert.Same(current.Rectangle, changed.Rectangle);
        Assert.Equal(0.7, changed.AreaSquareMeters);
        Assert.True(changed.IsLocked);
        Assert.Equal("Грядка", current.Details.Name);
    }

    [Fact]
    public void ApplyTo_ResizeDraft_MergesFreshPositionAndComputesArea()
    {
        var current = State() with { Rectangle = CanvasRectangle.Create(30, 45, 90, 90).ValueOrThrow() };
        var changed = Edit(Original() with { Width = 180 }).ApplyTo(current, new PlotLayout([])).ValueOrThrow();
        Assert.Equal((30.0, 45.0, 180.0, 90.0), (changed.Rectangle.X, changed.Rectangle.Y, changed.Rectangle.Width, changed.Rectangle.Height));
        Assert.Equal(0.7, changed.AreaSquareMeters);
    }

    [Fact]
    public void ApplyTo_LockedGeometryChange_Refuses()
        => Assert.True(Edit(Original() with { X = 30 }).ApplyTo(State() with { IsLocked = true }, new PlotLayout([])).IsFailure);

    [Fact]
    public void ApplyTo_NewCollision_RefusesEntireEdit()
        => Assert.True(Edit(Original() with { X = 120, Details = Details("Не сохранять") })
            .ApplyTo(State(), new PlotLayout([CanvasRectangle.Create(120, 0, 90, 90).ValueOrThrow()])).IsFailure);

    [Fact]
    public void ApplyTo_ClearedAndEmptyOptionalFields_PreservesDifference()
    {
        var result = Edit(Original() with { Details = Details() with { Note = Option.None<string>(), SoilType = Option.Some("") } })
            .ApplyTo(State(), new PlotLayout([])).ValueOrThrow();
        Assert.IsType<Option<string>.None>(result.Details.Note);
        Assert.Equal("", result.Details.SoilType.ToNullable());
    }

    [Theory]
    [InlineData(0, 90, 150)]
    [InlineData(1, -1, 150)]
    [InlineData(1, double.NaN, 150)]
    [InlineData(1, 90, 0)]
    public void Create_InvalidInput_Refuses(int id, double width, double scale)
        => Assert.True(ElementPropertiesEdit.Create(id, Original(), Original() with { Width = width }, scale).IsFailure);
}
