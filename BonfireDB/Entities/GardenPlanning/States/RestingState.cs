namespace BonfireDB.Entities.GardenPlanning.States;

public sealed class RestingState : GardenElementState
{
    public override string DisplayName   => "На зимовке";
    public override string StatusColor   => "#90A4AE";
    public override bool CanAddPlanting  => false;
    public override bool CanModifyGrid   => false;

    protected override IReadOnlyList<Type> AllowedTransitions =>
        [typeof(PlannedState), typeof(PreparedState)];
}
