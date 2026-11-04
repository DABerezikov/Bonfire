using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Application.Garden;

public sealed record DeleteGreenhouseFromViewModel(int GreenhouseId);

public sealed record GreenhouseDeletionResultFromViewModel(int GreenhouseId);

public interface IGreenhouseDeletionWriter
{
    Task<Result<GreenhouseDeletionResultFromViewModel>> DeleteAsync(GreenhouseId id,
        CancellationToken cancellationToken);
}

public interface IDeleteGreenhouse
{
    Task<Result<GreenhouseDeletionResultFromViewModel>> ExecuteAsync(DeleteGreenhouseFromViewModel request,
        CancellationToken cancellationToken = default);
}

public sealed class DeleteGreenhouse(IGreenhouseDeletionWriter writer) : IDeleteGreenhouse
{
    public Task<Result<GreenhouseDeletionResultFromViewModel>> ExecuteAsync(DeleteGreenhouseFromViewModel request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return GreenhouseId.Create(request.GreenhouseId).Match(
            id => writer.DeleteAsync(id, cancellationToken),
            error => Task.FromResult(Result.Failure<GreenhouseDeletionResultFromViewModel>(error)));
    }
}