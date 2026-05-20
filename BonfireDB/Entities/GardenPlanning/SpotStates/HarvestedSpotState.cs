namespace BonfireDB.Entities.GardenPlanning.SpotStates;

public sealed class HarvestedSpotState : PlantingSpotState
{
    public override string DisplayName      => "Убрано";
    public override string CellColor        => "#BBDEFB";
    public override string CellBorderColor  => "#1976D2";
    public override bool CanPlant           => true;
    public override bool CanHarvest         => false;

    protected override IReadOnlyList<Type> AllowedTransitions =>
        [typeof(EmptySpotState), typeof(PlantedSpotState)];
}
