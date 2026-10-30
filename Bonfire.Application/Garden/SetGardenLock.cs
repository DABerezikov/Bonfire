using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Application.Garden;

public sealed record SetGardenLockFromViewModel(int EntityId, GardenLockTarget Target, bool IsLocked);
public sealed record GardenLockResultFromViewModel(int Id, bool IsLocked);

public interface IGardenLockWriter
{
    Task<Result<GardenLockResultFromViewModel>> ApplyAsync(GardenLockChange change, CancellationToken cancellationToken);
}

public interface ISetGardenLock
{
    Task<Result<GardenLockResultFromViewModel>> ExecuteAsync(SetGardenLockFromViewModel request, CancellationToken cancellationToken = default);
}

public sealed class SetGardenLock(IGardenLockWriter writer) : ISetGardenLock
{
    public Task<Result<GardenLockResultFromViewModel>> ExecuteAsync(SetGardenLockFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return GardenLockChange.Create(request.EntityId, request.Target, request.IsLocked).Match(
            change => writer.ApplyAsync(change, cancellationToken),
            error => Task.FromResult(Result.Failure<GardenLockResultFromViewModel>(error)));
    }
}
