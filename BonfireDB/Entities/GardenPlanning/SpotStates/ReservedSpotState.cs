namespace BonfireDB.Entities.GardenPlanning.SpotStates;

public sealed class ReservedSpotState : PlantingSpotState
{
    public override string DisplayName      => "Запланировано";
    public override string CellColor        => "#FFF9C4";
    public override string CellBorderColor  => "#F9A825";
    public override bool CanPlant           => true;
    public override bool CanHarvest         => false;

    protected override IReadOnlyList<Type> AllowedTransitions =>
        [typeof(PlantedSpotState), typeof(EmptySpotState)];
}
