using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Application.Garden;

public sealed record DeleteGardenFromViewModel(int GardenId);
public sealed record GardenDeletionResultFromViewModel(int GardenId);

public interface IGardenDeletionWriter
{
    Task<Result<GardenDeletionResultFromViewModel>> DeleteAsync(GardenId id, CancellationToken cancellationToken);
}

public interface IDeleteGarden
{
    Task<Result<GardenDeletionResultFromViewModel>> ExecuteAsync(DeleteGardenFromViewModel request, CancellationToken cancellationToken = default);
}

public sealed class DeleteGarden(IGardenDeletionWriter writer) : IDeleteGarden
{
    public Task<Result<GardenDeletionResultFromViewModel>> ExecuteAsync(DeleteGardenFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return GardenId.Create(request.GardenId).Match(
            id => writer.DeleteAsync(id, cancellationToken),
            error => Task.FromResult(Result.Failure<GardenDeletionResultFromViewModel>(error)));
    }
}
