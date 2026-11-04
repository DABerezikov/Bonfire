using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Bonfire.Application.Libraries;

public sealed record UpdateSortFromViewModel(int Id, string Name, Option<int> CultureChange);

public sealed record SortPlantCultureFromViewModel(int PlantId, CultureDetailsFromViewModel Culture);

public sealed record SortDetailsFromViewModel(int Id, string Name, IReadOnlyList<SortPlantCultureFromViewModel> Plants);

public interface ISortWriter
{
    Task<Result<SortDetailsFromViewModel>> SaveAsync(SortLibraryEdit edit, CancellationToken cancellationToken);
}

public interface IUpdateSort
{
    Task<Result<SortDetailsFromViewModel>> ExecuteAsync(UpdateSortFromViewModel request,
        CancellationToken cancellationToken = default);
}

public sealed class UpdateSort(ISortWriter writer) : IUpdateSort
{
    public async Task<Result<SortDetailsFromViewModel>> ExecuteAsync(UpdateSortFromViewModel request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return await SortLibraryEdit.Create(request.Id, request.Name, request.CultureChange).Match(
            edit => writer.SaveAsync(edit, cancellationToken),
            error => Task.FromResult(Result.Failure<SortDetailsFromViewModel>(error))).ConfigureAwait(false);
    }
}