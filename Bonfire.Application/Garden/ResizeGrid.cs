using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Application.Garden;

public sealed record ResizeGridFromViewModel(int ElementId, int Rows, int Columns);
public sealed record GridSpotFromViewModel(int Id, int Row, int Column, string StateTypeName,
    Option<int> SeedlingInfoId, Option<DateTime> PlantedDate, Option<DateTime> HarvestDate, Option<string> Note);
public sealed record GridResultFromViewModel(int ElementId, int Rows, int Columns, IReadOnlyList<GridSpotFromViewModel> Spots);

public interface IGridWriter
{
    Task<Result<GridResultFromViewModel>> ApplyAsync(int elementId, GridSize size, CancellationToken cancellationToken);
}

public interface IResizeGrid
{
    Task<Result<GridResultFromViewModel>> ExecuteAsync(ResizeGridFromViewModel request, CancellationToken cancellationToken = default);
}

public sealed class ResizeGrid(IGridWriter writer) : IResizeGrid
{
    public Task<Result<GridResultFromViewModel>> ExecuteAsync(ResizeGridFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return request.ElementId <= 0
            ? Task.FromResult(Result.Failure<GridResultFromViewModel>("Некорректный идентификатор элемента"))
            : GridSize.Create(request.Rows, request.Columns).Match(
                size => writer.ApplyAsync(request.ElementId, size, cancellationToken),
                error => Task.FromResult(Result.Failure<GridResultFromViewModel>(error)));
    }
}
