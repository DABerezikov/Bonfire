namespace BonfireDB.Entities.GardenPlanning.States;

public sealed class PreparedState : GardenElementState
{
    public override string DisplayName   => "Подготовлена";
    public override string StatusColor   => "#FF9800";
    public override bool CanAddPlanting  => true;
    public override bool CanModifyGrid   => true;

    protected override IReadOnlyList<Type> AllowedTransitions =>
        [typeof(ActiveState), typeof(FallowState), typeof(ArchivedState)];
}
