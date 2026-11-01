using BonfireDB.Entities.Base;

namespace BonfireDB.Entities.Planning;

public class PerennialPlantingRow : NamedEntity
{
    public int Version { get; set; }
    public int EstablishmentYear { get; set; }
    public int? CultureId { get; set; }
    public int? SortId { get; set; }
    public int? RegionId { get; set; }
    public string DefinitionJson { get; set; } = "";
}

public class PerennialSeasonRow : Entity
{
    public int Version { get; set; }
    public int PlanId { get; set; }
    public int PlantingId { get; set; }
    public int SeasonYear { get; set; }
    public string SnapshotJson { get; set; } = "";
}