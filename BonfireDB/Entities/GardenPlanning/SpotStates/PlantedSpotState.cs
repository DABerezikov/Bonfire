namespace BonfireDB.Entities.GardenPlanning.SpotStates;

public sealed class PlantedSpotState : PlantingSpotState
{
    public override string DisplayName      => "Посажено";
    public override string CellColor        => "#C8E6C9";
    public override string CellBorderColor  => "#388E3C";
    public override bool CanPlant           => false;
    public override bool CanHarvest         => true;

    protected override IReadOnlyList<Type> AllowedTransitions =>
        [typeof(HarvestedSpotState), typeof(DeadSpotState)];
}
