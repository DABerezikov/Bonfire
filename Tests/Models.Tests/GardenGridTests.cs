using Bonfire.Domain.Garden;
using Bonfire.Domain.Garden.States;

namespace Models.Tests;

public class GardenGridTests
{
    [Fact]
    public void WithSize_Resize_PreservesSnapshotAndOnlyAddsMissingPositions()
    {
        var positions = new List<GridPosition> { new(0, 0), new(0, 1) };
        var grid = new GardenGrid(positions);
        positions.Clear();
        var change = grid.WithSize(GridSize.Create(2, 1).ValueOrThrow(), new PlannedState()).ValueOrThrow();
        Assert.Equal(new[] { new GridPosition(0, 1) }, change.Removed);
        Assert.Equal(new[] { new GridPosition(1, 0) }, change.Added);
        Assert.Equal(2, grid.Cells.Length);
    }

    [Fact]
    public void WithSize_SameSizeWithDuplicatePositions_DoesNotReplaceExistingCells()
    {
        var grid = new GardenGrid([new(0, 0), new(0, 0), new(1, 0)]);
        var change = grid.WithSize(GridSize.Create(2, 1).ValueOrThrow(), new PreparedState()).ValueOrThrow();
        Assert.Empty(change.Removed);
        Assert.Empty(change.Added);
        Assert.Equal(3, grid.Cells.Length);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(3, 0)]
    public void WithSize_ZeroDimension_RemovesAllExistingCells(int rows, int columns)
    {
        var change = new GardenGrid([new(0, 0)]).WithSize(GridSize.Create(rows, columns).ValueOrThrow(), new FallowState()).ValueOrThrow();
        Assert.Single(change.Removed);
        Assert.Empty(change.Added);
    }

    [Theory]
    [InlineData("ActiveState")]
    [InlineData("ArchivedState")]
    [InlineData("RestingState")]
    public void WithSize_BlockedState_ReturnsFailure(string state)
        => Assert.True(new GardenGrid([]).WithSize(GridSize.Create(1, 1).ValueOrThrow(), GardenElementState.From(state)).IsFailure);

    [Theory]
    [InlineData(-1, 3)]
    [InlineData(3, -1)]
    [InlineData(int.MaxValue, 2)]
    public void Create_InvalidSize_ReturnsFailure(int rows, int columns)
        => Assert.True(GridSize.Create(rows, columns).IsFailure);
}
