namespace BonfireDB.Entities.GardenPlanning.SpotStates;

public sealed class EmptySpotState : PlantingSpotState
{
    public override string DisplayName      => "Свободно";
    public override string CellColor        => "Transparent";
    public override string CellBorderColor  => "#BDBDBD";
    public override bool CanPlant           => true;
    public override bool CanHarvest         => false;

    protected override IReadOnlyList<Type> AllowedTransitions =>
        [typeof(ReservedSpotState), typeof(PlantedSpotState)];
}
