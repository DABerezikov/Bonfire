using BonfireDB.Entities.Base;

namespace BonfireDB.Entities.Planning;

public class AnnualWindowRow
{
    public int StartMonth { get; set; }
    public int StartDay { get; set; }
    public int EndMonth { get; set; }
    public int EndDay { get; set; }
    public bool EndInNextYear { get; set; }
}

public class DayRangeRow
{
    public int Min { get; set; }
    public int Max { get; set; }
}

public class PlanningExtrasRow
{
    // 0 — наследовать, 1 — отключить, 2 — включить. Не путать отсутствие с нулевым сроком.
    public int PrickingMode { get; set; }
    public DayRangeRow? PrickingOut { get; set; }
    public int HardeningMode { get; set; }
    public DayRangeRow? Hardening { get; set; }
    public decimal? GerminationPercentage { get; set; }
    public decimal? LossPercentage { get; set; }
    public int? LunarGroup { get; set; }
    public decimal? ReferenceSoilTemperatureC { get; set; }
    public decimal? YieldPerPlantKg { get; set; }
    public decimal? YieldPerM2Kg { get; set; }
    public string? LifeCycleJson { get; set; }
}

public class PlanningRegionRow : NamedEntity
{
    public string? StableKey { get; set; }
    public string TimeZoneId { get; set; } = "";
    public AnnualWindowRow? LastSpringFrost { get; set; }
    public AnnualWindowRow? FirstAutumnFrost { get; set; }
    public string? Note { get; set; }
    public string? Source { get; set; }
    public bool IsArchived { get; set; }
}

public class CultureGrowingProfileRow : Entity
{
    public int CultureId { get; set; }
    public DayRangeRow? Germination { get; set; }
    public DayRangeRow? SeedlingAge { get; set; }
    public DayRangeRow? Vegetation { get; set; }
    public PlanningExtrasRow Extras { get; set; } = new();
}

public class SortPlanningSettingsRow : Entity
{
    public int SortId { get; set; }
    public PlanningExtrasRow Extras { get; set; } = new();
}

public class RegionalPlantingWindowRow : Entity
{
    public int RegionId { get; set; }
    public int CultureId { get; set; }
    public int Location { get; set; }
    public int Placement { get; set; }
    public int Purpose { get; set; }
    public AnnualWindowRow Window { get; set; } = new();
    public string? Source { get; set; }
    public string? Note { get; set; }
}
