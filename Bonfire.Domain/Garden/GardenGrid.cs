using System.Collections.Immutable;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden.States;

namespace Bonfire.Domain.Garden;

public sealed record GridPosition(int Row, int Column);

public sealed record GridSize
{
    public int Rows { get; }
    public int Columns { get; }
    private GridSize(int rows, int columns) => (Rows, Columns) = (rows, columns);
    public static Result<GridSize> Create(int rows, int columns)
        => rows < 0 || columns < 0 || (long)rows * columns > int.MaxValue
            ? Result.Failure<GridSize>("Размеры сетки должны быть неотрицательными, число ячеек — в допустимом диапазоне")
            : Result.Success(new GridSize(rows, columns));
}

public sealed class GardenGrid
{
    public ImmutableArray<GridPosition> Cells { get; }
    public GardenGrid(IEnumerable<GridPosition> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        Cells = cells.ToImmutableArray();
    }

    public Result<GridChange> WithSize(GridSize size, GardenElementState state)
    {
        ArgumentNullException.ThrowIfNull(size);
        ArgumentNullException.ThrowIfNull(state);
        if (!state.CanModifyGrid)
            return Result.Failure<GridChange>($"В состоянии «{state.DisplayName}» изменение сетки запрещено");
        var occupied = Cells.ToHashSet();
        var positions = size.Rows == 0 || size.Columns == 0
            ? Enumerable.Empty<GridPosition>()
            : Enumerable.Range(0, size.Rows).SelectMany(row => Enumerable.Range(0, size.Columns).Select(column => new GridPosition(row, column)));
        return Result.Success(new GridChange(size,
            Cells.Where(cell => cell.Row >= size.Rows || cell.Column >= size.Columns).ToImmutableArray(),
            positions.Where(cell => !occupied.Contains(cell)).ToImmutableArray()));
    }
}

public sealed class GridChange
{
    public GridSize Size { get; }
    public ImmutableArray<GridPosition> Removed { get; }
    public ImmutableArray<GridPosition> Added { get; }
    internal GridChange(GridSize size, ImmutableArray<GridPosition> removed, ImmutableArray<GridPosition> added)
        => (Size, Removed, Added) = (size, removed, added);
}
