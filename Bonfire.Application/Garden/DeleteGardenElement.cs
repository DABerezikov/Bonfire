using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Application.Garden;

public sealed record DeleteGardenElementFromViewModel(int ElementId);
public sealed record ElementDeletionResultFromViewModel(int ElementId);

public interface IElementDeletionWriter
{
    Task<Result<ElementDeletionResultFromViewModel>> DeleteAsync(GardenElementId id, CancellationToken cancellationToken);
}

public interface IDeleteGardenElement
{
    Task<Result<ElementDeletionResultFromViewModel>> ExecuteAsync(DeleteGardenElementFromViewModel request, CancellationToken cancellationToken = default);
}

public sealed class DeleteGardenElement(IElementDeletionWriter writer) : IDeleteGardenElement
{
    public Task<Result<ElementDeletionResultFromViewModel>> ExecuteAsync(DeleteGardenElementFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return GardenElementId.Create(request.ElementId).Match(
            id => writer.DeleteAsync(id, cancellationToken),
            error => Task.FromResult(Result.Failure<ElementDeletionResultFromViewModel>(error)));
    }
}
