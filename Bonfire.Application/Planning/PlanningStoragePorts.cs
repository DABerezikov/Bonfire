using System.Collections.Immutable;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Planning.Regions;

namespace Bonfire.Application.Planning;

public sealed record PlanningBatchSettings
{
    public int SearchLimitDays { get; init; } = 35;
    public HarvestPlanningInput Harvest { get; init; } = new();
    public PlanningOverrides Overrides { get; init; } = new();
    public Option<EffectivePlanningExtras> Extras { get; init; } = Option.None<EffectivePlanningExtras>();
    public Option<GerminationPercentage> GerminationPercentage { get; init; } = Option.None<GerminationPercentage>();
    public Option<LossPercentage> LossPercentage { get; init; } = Option.None<LossPercentage>();
    public LunarGroup Group { get; init; } = LunarGroup.Unspecified;
}

public sealed record PlanningReferenceSnapshot(string CultureName, string SortName, string RegionName,
    string TimeZoneId, ImmutableArray<RegionalPlantingWindow> RegionalWindows);

public sealed record PlanningBatchDocument
{
    public PlannedBatchDefinition Batch { get; }
    public PlanningBatchSettings Settings { get; }
    public PlanningReferenceSnapshot References { get; }
    public SowingPreviewFromViewModel Preview { get; }
    public Option<SeedQuantityCalculation> Quantity { get; }
    public DateTimeOffset CalculatedAt { get; }
    public int CalculationVersion { get; }
    public int LunarPolicyVersion { get; }

    private PlanningBatchDocument(PlannedBatchDefinition batch, PlanningBatchSettings settings, PlanningReferenceSnapshot references,
        SowingPreviewFromViewModel preview, Option<SeedQuantityCalculation> quantity, DateTimeOffset calculatedAt, int calculationVersion, int lunarPolicyVersion)
        => (Batch, Settings, References, Preview, Quantity, CalculatedAt, CalculationVersion, LunarPolicyVersion)
            = (batch, settings, references, preview, quantity, calculatedAt, calculationVersion, lunarPolicyVersion);

    public static Result<PlanningBatchDocument> Create(PlannedBatchDefinition batch, PlanningBatchSettings settings,
        PlanningReferenceSnapshot references, SowingPreviewFromViewModel preview, Option<SeedQuantityCalculation> quantity,
        DateTimeOffset calculatedAt, int calculationVersion = EffectiveGrowingProfile.CalculationVersion, int lunarPolicyVersion = LunarSowingPolicy.Version)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(references);
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentNullException.ThrowIfNull(quantity);
        if (!Enum.IsDefined(settings.Group) || settings.SearchLimitDays < 0 || calculationVersion <= 0 || lunarPolicyVersion <= 0 || references.RegionalWindows.IsDefault)
            return Result.Failure<PlanningBatchDocument>("Некорректные параметры или версия снимка партии");
        if (settings.Extras.Match(x => x.LifeCycle.Match(life => !Enum.IsDefined(life.Source) || life.Value.Validate().IsFailure, () => false), () => false))
            return Result.Failure<PlanningBatchDocument>("Неверный профиль жизненного цикла снимка");
        return HarvestProjection.Calculate(settings.Harvest, batch.TargetPlants.Value)
            .Bind(_ => LibraryName.Create(references.CultureName)).Bind(_ => LibraryName.Create(references.SortName))
            .Bind(_ => RegionDefinition.Create(references.RegionName, references.TimeZoneId, RegionReference.Empty))
            .Bind(_ => Result.Success(new PlanningBatchDocument(batch, settings, references, preview, quantity,
                calculatedAt.ToUniversalTime(), calculationVersion, lunarPolicyVersion)));
    }
}

public sealed record PlannedBatchWrite(Option<PlannedBatchId> Id, PlanningBatchDocument Document);

public sealed class PlantingPlanWrite
{
    public Option<SeasonalPlantingPlanId> Id { get; }
    public int ExpectedVersion { get; }
    public SeasonalPlanDefinition Definition { get; }
    public ImmutableArray<PlannedBatchWrite> Batches { get; }

    private PlantingPlanWrite(Option<SeasonalPlantingPlanId> id, int version, SeasonalPlanDefinition definition, ImmutableArray<PlannedBatchWrite> batches)
        => (Id, ExpectedVersion, Definition, Batches) = (id, version, definition, batches);

    public static Result<PlantingPlanWrite> Create(Option<SeasonalPlantingPlanId> id, int expectedVersion,
        SeasonalPlanDefinition definition, ImmutableArray<PlannedBatchWrite> batches)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(definition);
        if (id.Match(_ => expectedVersion <= 0, () => expectedVersion != 0))
            return Result.Failure<PlantingPlanWrite>("Некорректная ожидаемая версия плана");
        if (batches.IsDefault || batches.Any(x => x is null || x.Document is null || x.Id is null))
            return Result.Failure<PlantingPlanWrite>("Не заданы партии плана");
        var ids = batches.Where(x => x.Id is Option<PlannedBatchId>.Some).Select(x => ((Option<PlannedBatchId>.Some)x.Id).Value).ToArray();
        if (ids.Distinct().Count() != ids.Length || id is Option<SeasonalPlantingPlanId>.None && ids.Length > 0)
            return Result.Failure<PlantingPlanWrite>("Повторяющиеся или чужие идентификаторы партий");
        return Result.Success(new PlantingPlanWrite(id, expectedVersion, definition, batches));
    }
}

public sealed record PlantingPlanSummaryFromViewModel(int Id, int Version, string Name, int SeasonYear, string RegionName);
public sealed record StoredPlannedBatchFromViewModel(int Id, int Version, Option<int> CurrentCultureId, Option<int> CurrentSortId, PlanningBatchDocument Document);
public sealed record PlantingPlanDetailsFromViewModel(int Id, int Version, Option<int> CurrentRegionId,
    SeasonalPlanDefinition Definition, ImmutableArray<StoredPlannedBatchFromViewModel> Batches);

public interface IPlantingPlanReader
{
    Task<Result<ImmutableArray<PlantingPlanSummaryFromViewModel>>> ReadAllAsync(CancellationToken cancellationToken);
    Task<Result<Option<PlantingPlanDetailsFromViewModel>>> ReadAsync(SeasonalPlantingPlanId id, CancellationToken cancellationToken);
}

public interface IPlantingPlanWriter
{
    Task<Result<PlantingPlanDetailsFromViewModel>> SaveAsync(PlantingPlanWrite request, CancellationToken cancellationToken);
    Task<Result<int>> DeleteAsync(SeasonalPlantingPlanId id, int expectedVersion, CancellationToken cancellationToken);
}
