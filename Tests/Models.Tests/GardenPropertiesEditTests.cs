using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Models.Tests;

public class GardenPropertiesEditTests
{
    private static GardenEditorValues Values() => new("Участок", Option.Some("Исходная"), Option.None<string>(), 6, 3);
    private static GardenPropertiesState State() => new(1, Values(), CanvasRectangle.Create(0, 0, 900, 450).ValueOrThrow());
    private static GardenPropertiesEdit Edit(GardenEditorValues values) => GardenPropertiesEdit.Create(1, Values(), values, 150).ValueOrThrow();

    [Fact]
    public void ApplyTo_MetadataOnly_PreservesFreshDimensionsCanvasAndUntouchedNote()
    {
        var current = State() with { Values = Values() with { WidthMeters = 8, Note = Option.Some("Свежая") }, Canvas = CanvasRectangle.Create(0, 0, 1200, 450).ValueOrThrow() };
        var saved = Edit(Values() with { Name = "  Новое  " }).ApplyTo(current, new GardenContents([], [])).ValueOrThrow();
        Assert.Equal("  Новое  ", saved.Values.Name);
        Assert.Equal("Свежая", saved.Values.Note.ToNullable());
        Assert.Equal(8, saved.Values.WidthMeters);
        Assert.Same(current.Canvas, saved.Canvas);
        Assert.Equal("Участок", current.Values.Name);
    }

    [Fact]
    public void ApplyTo_WidthChange_UsesFreshHeightAndConvertsToCanvasUnits()
    {
        var current = State() with { Values = Values() with { HeightMeters = 4 } };
        var saved = Edit(Values() with { WidthMeters = 7 }).ApplyTo(current, new GardenContents([], [])).ValueOrThrow();
        Assert.Equal((7.0, 4.0, 1050.0, 600.0), (saved.Values.WidthMeters, saved.Values.HeightMeters, saved.Canvas.Width, saved.Canvas.Height));
    }

    [Fact]
    public void ApplyTo_InvalidFreshDimensions_RefusesWithoutPreservingInvalidValues()
        => Assert.True(Edit(Values() with { Name = "Новое" }).ApplyTo(State() with
            { Values = Values() with { HeightMeters = double.PositiveInfinity } }, new GardenContents([], [])).IsFailure);

    [Fact]
    public void ApplyTo_ChildOutsideNewBounds_RefusesWithName()
    {
        var contents = new GardenContents([new("Рассада", CanvasRectangle.Create(700, 0, 100, 90).ValueOrThrow())], []);
        var result = Edit(Values() with { WidthMeters = 5 }).ApplyTo(State(), contents);
        Assert.Contains("Рассада", result.Match(_ => "", error => error));
    }

    [Fact]
    public void ApplyTo_ExteriorGreenhouseOutsideBounds_RefusesWithName()
    {
        var contents = new GardenContents([], [new("Снаружи", CanvasRectangle.Create(700, 0, 100, 90).ValueOrThrow())]);
        var result = Edit(Values() with { WidthMeters = 5 }).ApplyTo(State(), contents);
        Assert.Contains("теплица «Снаружи»", result.Match(_ => "", error => error));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 3)]
    public void ApplyTo_EmptyZeroSize_Allows(double width, double height)
    {
        var result = Edit(Values() with { WidthMeters = width, HeightMeters = height }).ApplyTo(State(), new GardenContents([], [])).ValueOrThrow();
        Assert.Equal((width * 150, height * 150), (result.Canvas.Width, result.Canvas.Height));
    }

    [Theory]
    [InlineData(-1, 150)]
    [InlineData(double.NaN, 150)]
    [InlineData(double.PositiveInfinity, 150)]
    [InlineData(double.MaxValue, 150)]
    [InlineData(6, 0)]
    [InlineData(6, double.NaN)]
    public void Create_InvalidDimensionsOrScale_Refuses(double width, double scale)
        => Assert.True(GardenPropertiesEdit.Create(1, Values(), Values() with { WidthMeters = width }, scale).IsFailure);

    [Fact]
    public void RequireFit_SnapshotsChildrenAndAllowsNegativePositionAndTouchingEdges()
    {
        var elements = new List<InteriorElement> { new("Край", CanvasRectangle.Create(-10, 0, 910, 450).ValueOrThrow()) };
        var contents = new GardenContents(elements, []);
        elements.Clear();
        Assert.Single(contents.Elements);
        Assert.True(contents.RequireFit(State().Canvas).IsSuccess);
    }
    [Fact]
    public void ApplyTo_MetadataOnly_StillChecksBothKindsAndPrioritizesElements()
    {
        var outside = CanvasRectangle.Create(1000, 0, 100, 90).ValueOrThrow();
        var result = Edit(Values() with { Name = "Новое" }).ApplyTo(State(),
            new GardenContents([new("Грядка", outside)], [new("Теплица", outside)]));
        Assert.Contains("элемент «Грядка»", result.Match(_ => "", error => error));
    }

    [Fact]
    public void ApplyTo_WrongIdentity_Refuses()
        => Assert.True(Edit(Values()).ApplyTo(State() with { Id = 2 }, new GardenContents([], [])).IsFailure);

    [Fact]
    public void RequireFit_SnapshotsGreenhousesAndAllowsTouchingEdges()
    {
        var houses = new List<InteriorElement> { new("Край", CanvasRectangle.Create(-10, 0, 910, 450).ValueOrThrow()) };
        var contents = new GardenContents([], houses);
        houses.Clear();
        Assert.Single(contents.Greenhouses);
        Assert.True(contents.RequireFit(State().Canvas).IsSuccess);
    }

}
