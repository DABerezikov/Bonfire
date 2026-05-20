namespace BonfireDB.Entities.GardenPlanning.States;

public sealed class ActiveState : GardenElementState
{
    public override string DisplayName   => "Активна";
    public override string StatusColor   => "#4CAF50";
    public override bool CanAddPlanting  => true;
    // Запрещаем изменение сетки при живых посадках
    public override bool CanModifyGrid   => false;

    protected override IReadOnlyList<Type> AllowedTransitions =>
        [typeof(FallowState), typeof(RestingState)];
}
