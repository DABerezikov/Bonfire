using System.Collections.Immutable;
using Bonfire.Application.Libraries;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Planning.Regions;
using Bonfire.Domain.Seeds;

namespace Bonfire.Application.Planning;

public sealed record PlanningChoiceFromViewModel(int CultureId, string CultureName, int SortId, string SortName);

public sealed record PlanningContextFromViewModel(
    RegionFromViewModel Region,
    CultureDetailsFromViewModel Culture,
    int SortId,
    string SortName,
    SortCharacteristicsValues SortCharacteristics,
    Option<CultureGrowingProfile> CultureProfile,
    Option<SortPlanningSettings> SortSettings,
    ImmutableArray<RegionalPlantingWindow> RegionalWindows,
    string Fingerprint);

public interface IPlanningContextReader
{
    Task<Result<ImmutableArray<PlanningChoiceFromViewModel>>> ReadChoicesAsync(CancellationToken cancellationToken);

    Task<Result<PlanningContextFromViewModel>> ReadAsync(RegionId regionId, CultureId cultureId, SortId sortId,
        CancellationToken cancellationToken);
}

public sealed record PlanningBatchInputFromViewModel(
    int RegionId,
    int CultureId,
    int SortId,
    GrowingConditions Conditions,
    GrowingMethod Method,
    PlantCount TargetPlants,
    PlanningOverrides Overrides,
    PlanningChoices Choices,
    Option<GerminationPercentage> GerminationOverride,
    Option<LossPercentage> LossOverride,
    Option<LunarGroup> LunarGroupOverride,
    int SearchLimitDays)
{
    public HarvestPlanningInput Harvest { get; init; } = new();
    public Option<LifeCycleProfile> LifeCycleOverride { get; init; } = Option.None<LifeCycleProfile>();
}

/// <summary>Соответствие preview проверяется точным сравнением иммутабельного Input, а не хешем UI-ссылки.</summary>
public sealed record PlanningPreviewFromViewModel(
    PlanningBatchInputFromViewModel Input,
    string ContextFingerprint,
    PlanningReferenceSnapshot References,
    Option<PlanningBatchDocument> Document,
    ImmutableArray<string> MissingInputs,
    DateTimeOffset CalculatedAt);

public interface ICalculatePlannedBatch
{
    Task<Result<PlanningPreviewFromViewModel>> ExecuteAsync(PlanningBatchInputFromViewModel input,
        CancellationToken cancellationToken = default);
}