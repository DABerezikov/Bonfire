namespace BonfireDB.Entities.GardenPlanning.States;

public sealed class PlannedState : GardenElementState
{
    public override string DisplayName   => "Запланирована";
    public override string StatusColor   => "#9E9E9E";
    public override bool CanAddPlanting  => false;
    public override bool CanModifyGrid   => true;

    protected override IReadOnlyList<Type> AllowedTransitions =>
        [typeof(PreparedState), typeof(ArchivedState)];
}
