using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Domain.Garden.States;

namespace Bonfire.Application.Garden;

public sealed record ChangeGardenStateFromViewModel(int EntityId, StateChangeTarget Target);
public abstract record GardenStateResultFromViewModel
{
    private GardenStateResultFromViewModel() { }
    public sealed record Element(int Id, GardenElementState State) : GardenStateResultFromViewModel;
    public sealed record Greenhouse(int Id, GardenElementState State) : GardenStateResultFromViewModel;
    public sealed record Spot(GridSpotFromViewModel Cell) : GardenStateResultFromViewModel;
}

public interface IGardenStateWriter
{
    Task<Result<GardenStateResultFromViewModel>> ApplyAsync(GardenStateChange change, CancellationToken cancellationToken);
}

public interface IChangeGardenState
{
    Task<Result<GardenStateResultFromViewModel>> ExecuteAsync(ChangeGardenStateFromViewModel request, CancellationToken cancellationToken = default);
}

public sealed class ChangeGardenState(IGardenStateWriter writer) : IChangeGardenState
{
    public Task<Result<GardenStateResultFromViewModel>> ExecuteAsync(ChangeGardenStateFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return GardenStateChange.Create(request.EntityId, request.Target).Match(
            change => writer.ApplyAsync(change, cancellationToken),
            error => Task.FromResult(Result.Failure<GardenStateResultFromViewModel>(error)));
    }
}
