using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.Regions;

namespace Bonfire.Domain.Harvest;

public sealed record HarvestEntryId
{
    public int Value { get; }
    private HarvestEntryId(int value) => Value = value;
    public static Result<HarvestEntryId> Create(int value) => value > 0 ? Result.Success(new HarvestEntryId(value)) : Result.Failure<HarvestEntryId>("Неверный ID сбора");
}

public sealed record HarvestMass
{
    public decimal Kg { get; }
    private HarvestMass(decimal kg) => Kg = kg;
    public static Result<HarvestMass> Create(decimal kg) => kg > 0 ? Result.Success(new HarvestMass(kg)) : Result.Failure<HarvestMass>("Масса сбора должна быть положительной, в кг");
}

public sealed record HarvestIdentity
{
    public CultureId CultureId { get; }
    public SortId SortId { get; }
    public string CultureName { get; }
    public string SortName { get; }
    public int SeasonYear { get; }
    public string RegionName { get; }
    public string TimeZoneId { get; }
    public Option<PlannedBatchId> BatchId { get; }
    public Option<PerennialPlantingId> PerennialId { get; }
    private HarvestIdentity(CultureId cultureId, SortId sortId, string cultureName, string sortName, int seasonYear,
        string regionName, string timeZoneId, Option<PlannedBatchId> batchId, Option<PerennialPlantingId> perennialId)
        => (CultureId, SortId, CultureName, SortName, SeasonYear, RegionName, TimeZoneId, BatchId, PerennialId)
            = (cultureId, sortId, cultureName, sortName, seasonYear, regionName, timeZoneId, batchId, perennialId);

    public static Result<HarvestIdentity> Create(CultureId cultureId, SortId sortId, string cultureName, string sortName, int seasonYear,
        string regionName, string timeZoneId, Option<PlannedBatchId> batchId, Option<PerennialPlantingId> perennialId)
    {
        if (seasonYear is < 1 or > 9999 || batchId is Option<PlannedBatchId>.Some && perennialId is Option<PerennialPlantingId>.Some)
            return Result.Failure<HarvestIdentity>("Неверный сезон или одновременно выбраны партия и многолетняя посадка");
        return LibraryName.Create(cultureName).Bind(_ => LibraryName.Create(sortName))
            .Bind(_ => RegionDefinition.Create(regionName, timeZoneId, RegionReference.Empty))
            .Bind(_ => Result.Success(new HarvestIdentity(cultureId, sortId, cultureName, sortName, seasonYear, regionName, timeZoneId, batchId, perennialId)));
    }
}

public sealed record HarvestEntry
{
    public HarvestIdentity Identity { get; }
    public DateOnly Date { get; }
    public HarvestMass Mass { get; }
    public Option<string> Note { get; }
    private HarvestEntry(HarvestIdentity identity, DateOnly date, HarvestMass mass, Option<string> note) => (Identity, Date, Mass, Note) = (identity, date, mass, note);
    public static Result<HarvestEntry> Create(HarvestIdentity identity, DateOnly date, HarvestMass mass, Option<string> note, DateOnly today)
        => date > today ? Result.Failure<HarvestEntry>("Будущий сбор нельзя записать как факт") : Result.Success(new HarvestEntry(identity, date, mass, note));
}

public sealed record RecordedHarvest(HarvestEntryId Id, HarvestEntry Entry);
public sealed record HarvestSummary(int Count, decimal TotalKg, Option<DateOnly> FirstDate)
{
    public static Result<HarvestSummary> Calculate(IEnumerable<RecordedHarvest> entries)
    {
        var unique = entries.DistinctBy(x => x.Id).ToArray();
        try { return Result.Success(new HarvestSummary(unique.Length, unique.Sum(x => x.Entry.Mass.Kg),
            unique.Length > 0 ? Option.Some(unique.Min(x => x.Entry.Date)) : Option.None<DateOnly>())); }
        catch (OverflowException) { return Result.Failure<HarvestSummary>("Суммарная масса превышает допустимый диапазон"); }
    }
}
