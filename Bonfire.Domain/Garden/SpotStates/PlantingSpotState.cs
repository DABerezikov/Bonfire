using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Garden.SpotStates;

public abstract class PlantingSpotState
{
    private protected PlantingSpotState() { }

    public abstract string DisplayName { get; }
    public abstract string CellColor { get; }
    public abstract string CellBorderColor { get; }
    public abstract bool CanPlant { get; }
    public abstract bool CanHarvest { get; }

    protected abstract IReadOnlyList<Type> AllowedTransitions { get; }

    public bool CanTransitionTo(PlantingSpotState target)
        => AllowedTransitions.Contains(target.GetType());

    public Result<PlantingSpotState> TransitionTo(PlantingSpotState target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return CanTransitionTo(target)
            ? Result.Success(target)
            : Result.Failure<PlantingSpotState>($"Переход «{DisplayName}» → «{target.DisplayName}» запрещён");
    }

    public static PlantingSpotState From(string typeName) => typeName switch
    {
        nameof(EmptySpotState)     => new EmptySpotState(),
        nameof(ReservedSpotState)  => new ReservedSpotState(),
        nameof(PlantedSpotState)   => new PlantedSpotState(),
        nameof(HarvestedSpotState) => new HarvestedSpotState(),
        nameof(DeadSpotState)      => new DeadSpotState(),
        _                          => new EmptySpotState()
    };
}
