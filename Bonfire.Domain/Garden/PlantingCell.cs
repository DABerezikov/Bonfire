using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden.SpotStates;
using Bonfire.Domain.Garden.States;

namespace Bonfire.Domain.Garden;

public abstract record SpotChange
{
    private SpotChange() { }
    public sealed record Transition(PlantingSpotState State, Option<string> Label) : SpotChange;
    public sealed record Clear : SpotChange;
}

public sealed class PlantingCell
{
    public PlantingSpotState State { get; }
    public Option<int> SeedlingInfoId { get; }
    public Option<DateTime> PlantedDate { get; }
    public Option<DateTime> HarvestDate { get; }
    public Option<string> Note { get; }

    public PlantingCell(PlantingSpotState state, Option<int> seedlingInfoId, Option<DateTime> plantedDate, Option<DateTime> harvestDate, Option<string> note)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(seedlingInfoId);
        ArgumentNullException.ThrowIfNull(plantedDate);
        ArgumentNullException.ThrowIfNull(harvestDate);
        ArgumentNullException.ThrowIfNull(note);
        (State, SeedlingInfoId, PlantedDate, HarvestDate, Note) = (state, seedlingInfoId, plantedDate, harvestDate, note);
    }

    public Result<PlantingCell> Apply(SpotChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        return change switch
        {
            SpotChange.Transition transition => WithState(transition.State, transition.Label),
            SpotChange.Clear => Result.Success(new PlantingCell(new EmptySpotState(), Option.None<int>(),
                Option.None<DateTime>(), Option.None<DateTime>(), Note)),
            _ => throw new System.Diagnostics.UnreachableException()
        };
    }

    public Result<PlantingCell> WithState(PlantingSpotState state, Option<string> label)
    {
        ArgumentNullException.ThrowIfNull(label);
        return State.TransitionTo(state).Bind(changed => Result.Success(new PlantingCell(changed, SeedlingInfoId,
            PlantedDate, HarvestDate, label is Option<string>.Some ? label : Note)));
    }
}

public abstract record StateChangeTarget
{
    private StateChangeTarget() { }
    public sealed record Element(GardenElementState State) : StateChangeTarget;
    public sealed record Greenhouse(GardenElementState State) : StateChangeTarget;
    public sealed record Spot(SpotChange Change) : StateChangeTarget;
}

public sealed class GardenStateChange
{
    public int EntityId { get; }
    public StateChangeTarget Target { get; }
    private GardenStateChange(int entityId, StateChangeTarget target) => (EntityId, Target) = (entityId, target);

    public static Result<GardenStateChange> Create(int entityId, StateChangeTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        switch (target)
        {
            case StateChangeTarget.Element element: ArgumentNullException.ThrowIfNull(element.State); break;
            case StateChangeTarget.Greenhouse greenhouse: ArgumentNullException.ThrowIfNull(greenhouse.State); break;
            case StateChangeTarget.Spot spot:
                ArgumentNullException.ThrowIfNull(spot.Change);
                if (spot.Change is SpotChange.Transition transition)
                {
                    ArgumentNullException.ThrowIfNull(transition.State);
                    ArgumentNullException.ThrowIfNull(transition.Label);
                }
                break;
        }
        return entityId <= 0
            ? Result.Failure<GardenStateChange>("Некорректный идентификатор объекта планировщика")
            : Result.Success(new GardenStateChange(entityId, target));
    }
}
