using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Bonfire.Domain.Planning;

/// <summary>Проверенные сроки и их происхождение. Возраст и вегетация — от всходов.</summary>
public sealed record EffectiveGrowingProfile(
    SourcedValue<DayRange> Germination,
    Option<SourcedValue<DayRange>> SeedlingAge,
    Option<SourcedValue<DayRange>> Vegetation)
{
    public const int CalculationVersion = 1;
}

public sealed record PlanningOverrides
{
    public Option<DayRange> Germination { get; init; } = Option.None<DayRange>();
    public Option<DayRange> SeedlingAge { get; init; } = Option.None<DayRange>();
    public Option<DayRange> Vegetation { get; init; } = Option.None<DayRange>();
    public OperationOverride<DayRange> PrickingOut { get; init; } = new OperationOverride<DayRange>.Inherit();
    public OperationOverride<DayRange> Hardening { get; init; } = new OperationOverride<DayRange>.Inherit();
}

/// <summary>Ручные решения хранятся отдельно от пересчитываемого прогноза.</summary>
public sealed record PlanningChoices(
    Option<DateOnly> SowingDate,
    Option<DateOnly> TransplantDate,
    Option<PlantCount> ManualSeedCount)
{
    public static PlanningChoices Empty { get; } = new(Option.None<DateOnly>(), Option.None<DateOnly>(), Option.None<PlantCount>());
}

public sealed record PlannedBatch
{
    public PlannedBatchId Id { get; }
    public CultureId CultureId { get; }
    public SortId SortId { get; }
    public GrowingConditions Conditions { get; }
    public GrowingMethod Method { get; }
    public EffectiveGrowingProfile Profile { get; }
    public PlantCount TargetPlants { get; }
    public PlanningChoices Choices { get; }

    private PlannedBatch(PlannedBatchId id, CultureId cultureId, SortId sortId, GrowingConditions conditions,
        GrowingMethod method, EffectiveGrowingProfile profile, PlantCount targetPlants, PlanningChoices choices)
        => (Id, CultureId, SortId, Conditions, Method, Profile, TargetPlants, Choices)
            = (id, cultureId, sortId, conditions, method, profile, targetPlants, choices);

    public static Result<PlannedBatch> Create(PlannedBatchId id, CultureId cultureId, SortId sortId,
        GrowingConditions conditions, GrowingMethod method, EffectiveGrowingProfile profile,
        PlantCount targetPlants, PlanningChoices choices)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(cultureId);
        ArgumentNullException.ThrowIfNull(sortId);
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(targetPlants);
        ArgumentNullException.ThrowIfNull(choices);
        if (method is GrowingMethod.Transplant && profile.SeedlingAge is Option<SourcedValue<DayRange>>.None)
            return Result.Failure<PlannedBatch>("Не задан возраст рассады от всходов");
        if (method is GrowingMethod.DirectSow && choices.TransplantDate is Option<DateOnly>.Some)
            return Result.Failure<PlannedBatch>("Для прямого посева дата высадки неприменима");
        return Result.Success(new PlannedBatch(id, cultureId, sortId, conditions, method, profile, targetPlants, choices));
    }

    public Result<PlannedBatch> WithChoices(PlanningChoices choices)
        => Create(Id, CultureId, SortId, Conditions, Method, Profile, TargetPlants, choices);
}
