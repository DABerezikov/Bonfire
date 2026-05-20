namespace BonfireDB.Entities.GardenPlanning.States;

public abstract class GardenElementState
{
    public abstract string DisplayName { get; }
    public abstract string StatusColor { get; }
    public abstract bool CanAddPlanting { get; }
    public abstract bool CanModifyGrid { get; }

    protected abstract IReadOnlyList<Type> AllowedTransitions { get; }

    public bool CanTransitionTo(GardenElementState target)
        => AllowedTransitions.Contains(target.GetType());

    public static GardenElementState From(string typeName) => typeName switch
    {
        nameof(PlannedState)  => new PlannedState(),
        nameof(PreparedState) => new PreparedState(),
        nameof(ActiveState)   => new ActiveState(),
        nameof(FallowState)   => new FallowState(),
        nameof(RestingState)  => new RestingState(),
        nameof(ArchivedState) => new ArchivedState(),
        _                     => new PlannedState()
    };
}
