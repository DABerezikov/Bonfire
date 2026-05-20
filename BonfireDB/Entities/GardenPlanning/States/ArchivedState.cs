namespace BonfireDB.Entities.GardenPlanning.States;

public sealed class ArchivedState : GardenElementState
{
    public override string DisplayName   => "В архиве";
    public override string StatusColor   => "#BDBDBD";
    public override bool CanAddPlanting  => false;
    public override bool CanModifyGrid   => false;

    protected override IReadOnlyList<Type> AllowedTransitions =>
        [typeof(PlannedState)];
}
