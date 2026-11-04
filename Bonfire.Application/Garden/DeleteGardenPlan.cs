using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Application.Garden;

public sealed record DeleteGardenPlanFromViewModel(int GardenPlanId);

public sealed record GardenPlanDeletionResultFromViewModel(int GardenPlanId);

public interface IGardenPlanDeletionWriter
{
    Task<Result<GardenPlanDeletionResultFromViewModel>> DeleteAsync(GardenPlanId id,
        CancellationToken cancellationToken);
}

public interface IDeleteGardenPlan
{
    Task<Result<GardenPlanDeletionResultFromViewModel>> ExecuteAsync(DeleteGardenPlanFromViewModel request,
        CancellationToken cancellationToken = default);
}

public sealed class DeleteGardenPlan(IGardenPlanDeletionWriter writer) : IDeleteGardenPlan
{
    public Task<Result<GardenPlanDeletionResultFromViewModel>> ExecuteAsync(DeleteGardenPlanFromViewModel request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return GardenPlanId.Create(request.GardenPlanId).Match(
            id => writer.DeleteAsync(id, cancellationToken),
            error => Task.FromResult(Result.Failure<GardenPlanDeletionResultFromViewModel>(error)));
    }
}