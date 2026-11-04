using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Bonfire.Domain.Planning.GrowingProfiles;

public sealed record GrowingProfileParameters
{
    public Option<DayRange> Germination { get; init; } = Option.None<DayRange>();
    public Option<DayRange> SeedlingAge { get; init; } = Option.None<DayRange>();
    public Option<DayRange> Vegetation { get; init; } = Option.None<DayRange>();
}

/// <summary>Новые настройки. Сроки сорта остаются в SortCharacteristicsValues.</summary>
public sealed record PlanningExtras
{
    public OperationOverride<DayRange> PrickingOut { get; init; } = new OperationOverride<DayRange>.Inherit();
    public OperationOverride<DayRange> Hardening { get; init; } = new OperationOverride<DayRange>.Inherit();
    public Option<GerminationPercentage> GerminationPercentage { get; init; } = Option.None<GerminationPercentage>();
    public Option<LossPercentage> LossPercentage { get; init; } = Option.None<LossPercentage>();
    public Option<LunarGroup> LunarGroup { get; init; } = Option.None<LunarGroup>();
    public Option<decimal> ReferenceSoilTemperatureC { get; init; } = Option.None<decimal>();
    public Option<decimal> YieldPerPlantKg { get; init; } = Option.None<decimal>();
    public Option<decimal> YieldPerM2Kg { get; init; } = Option.None<decimal>();
    public Option<LifeCycleProfile> LifeCycle { get; init; } = Option.None<LifeCycleProfile>();

    public bool HasValidYield() =>
        YieldPerPlantKg.Match(x => x >= 0, () => true) && YieldPerM2Kg.Match(x => x >= 0, () => true);
}

public sealed record CultureGrowingProfile
{
    private CultureGrowingProfile(CultureId cultureId, GrowingProfileParameters parameters, PlanningExtras extras)
        => (CultureId, Parameters, Extras) = (cultureId, parameters, extras);

    public CultureId CultureId { get; }
    public GrowingProfileParameters Parameters { get; }
    public PlanningExtras Extras { get; }

    public static Result<CultureGrowingProfile> Create(CultureId cultureId, GrowingProfileParameters parameters,
        PlanningExtras extras)
    {
        ArgumentNullException.ThrowIfNull(cultureId);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(extras);
        return extras.HasValidYield() && extras.LifeCycle.Match(x => x.Validate().IsSuccess, () => true) &&
               extras.LunarGroup.Match(Enum.IsDefined, () => true)
            ? Result.Success(new CultureGrowingProfile(cultureId, parameters, extras))
            : Result.Failure<CultureGrowingProfile>("Неверная лунная группа или норма урожайности");
    }
}

public sealed record SortPlanningSettings
{
    private SortPlanningSettings(SortId sortId, PlanningExtras extras) => (SortId, Extras) = (sortId, extras);
    public SortId SortId { get; }
    public PlanningExtras Extras { get; }

    public static Result<SortPlanningSettings> Create(SortId sortId, PlanningExtras extras)
    {
        ArgumentNullException.ThrowIfNull(sortId);
        ArgumentNullException.ThrowIfNull(extras);
        return extras.HasValidYield() && extras.LifeCycle.Match(x => x.Validate().IsSuccess, () => true) &&
               extras.LunarGroup.Match(Enum.IsDefined, () => true)
            ? Result.Success(new SortPlanningSettings(sortId, extras))
            : Result.Failure<SortPlanningSettings>("Неверная лунная группа или норма урожайности");
    }
}

public abstract record OperationPlan<T>
{
    private OperationPlan()
    {
    }

    public sealed record Unknown : OperationPlan<T>;

    public sealed record Disabled(ParameterSource Source) : OperationPlan<T>;

    public sealed record Enabled(SourcedValue<T> Setting) : OperationPlan<T>;
}

public sealed record EffectivePlanningExtras(
    OperationPlan<DayRange> PrickingOut,
    OperationPlan<DayRange> Hardening,
    Option<SourcedValue<GerminationPercentage>> GerminationPercentage,
    Option<SourcedValue<LossPercentage>> LossPercentage,
    Option<SourcedValue<LunarGroup>> LunarGroup,
    Option<SourcedValue<decimal>> ReferenceSoilTemperatureC)
{
    public Option<SourcedValue<decimal>> YieldPerPlantKg { get; init; } = Option.None<SourcedValue<decimal>>();
    public Option<SourcedValue<decimal>> YieldPerM2Kg { get; init; } = Option.None<SourcedValue<decimal>>();

    public Option<SourcedValue<LifeCycleProfile>> LifeCycle { get; init; } =
        Option.None<SourcedValue<LifeCycleProfile>>();
}