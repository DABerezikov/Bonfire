namespace BonfireDB.Entities.GardenPlanning.SpotStates;

public sealed class DeadSpotState : PlantingSpotState
{
    public override string DisplayName      => "Погибло";
    public override string CellColor        => "#FFCDD2";
    public override string CellBorderColor  => "#C62828";
    public override bool CanPlant           => true;
    public override bool CanHarvest         => false;

    protected override IReadOnlyList<Type> AllowedTransitions =>
        [typeof(EmptySpotState)];
}
