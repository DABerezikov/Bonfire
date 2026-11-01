using BonfireDB.Entities.Base;
namespace BonfireDB.Entities.Planning;
public class HarvestEntryRow : Entity
{
    public int Version { get; set; }
    public DateOnly Date { get; set; }
    public decimal MassKg { get; set; }
    public string? Note { get; set; }
    public int SeasonYear { get; set; }
    public string RegionName { get; set; } = "";
    public string TimeZoneId { get; set; } = "";
    public int SnapshotCultureId { get; set; }
    public int SnapshotSortId { get; set; }
    public string CultureName { get; set; } = "";
    public string SortName { get; set; } = "";
    public int? SnapshotBatchId { get; set; }
    public int? SnapshotPerennialId { get; set; }
    public int? CultureId { get; set; }
    public int? SortId { get; set; }
    public int? BatchId { get; set; }
    public int? PerennialId { get; set; }
}
