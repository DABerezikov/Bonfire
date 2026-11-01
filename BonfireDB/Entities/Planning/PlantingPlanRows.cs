using BonfireDB.Entities.Base;

namespace BonfireDB.Entities.Planning;

public class SeasonalPlantingPlanRow : NamedEntity
{
    public int SeasonYear { get; set; }
    public int? RegionId { get; set; }
    public int SnapshotRegionId { get; set; }
    public string RegionName { get; set; } = "";
    public string TimeZoneId { get; set; } = "";
    public int Version { get; set; }
    public ICollection<PlannedBatchRow> Batches { get; set; } = [];
}

public class PlannedBatchRow : Entity
{
    public int PlanId { get; set; }
    public SeasonalPlantingPlanRow Plan { get; set; } = null!;
    public int Ordinal { get; set; }
    public int? CultureId { get; set; }
    public int? SortId { get; set; }
    public int Location { get; set; }
    public int Placement { get; set; }
    public string Method { get; set; } = "";
    public DateOnly? SelectedSowingDate { get; set; }
    public int Version { get; set; }
    public string SnapshotJson { get; set; } = "";
}
