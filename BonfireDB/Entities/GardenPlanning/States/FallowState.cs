namespace BonfireDB.Entities.GardenPlanning.States;

public sealed class FallowState : GardenElementState
{
    public override string DisplayName   => "Под паром";
    public override string StatusColor   => "#795548";
    public override bool CanAddPlanting  => false;
    public override bool CanModifyGrid   => true;

    protected override IReadOnlyList<Type> AllowedTransitions =>
        [typeof(PreparedState), typeof(RestingState), typeof(ArchivedState)];
}
