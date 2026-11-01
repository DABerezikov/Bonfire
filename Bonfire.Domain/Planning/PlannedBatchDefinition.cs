using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Bonfire.Domain.Planning;

/// <summary>Проверенная партия до назначения ID хранилищем.</summary>
public sealed record PlannedBatchDefinition
{
    public CultureId CultureId { get; }
    public SortId SortId { get; }
    public GrowingConditions Conditions { get; }
    public GrowingMethod Method { get; }
    public EffectiveGrowingProfile Profile { get; }
    public PlantCount TargetPlants { get; }
    public PlanningChoices Choices { get; }

    private PlannedBatchDefinition(CultureId cultureId, SortId sortId, GrowingConditions conditions,
        GrowingMethod method, EffectiveGrowingProfile profile, PlantCount targetPlants, PlanningChoices choices)
        => (CultureId, SortId, Conditions, Method, Profile, TargetPlants, Choices)
            = (cultureId, sortId, conditions, method, profile, targetPlants, choices);

    public static Result<PlannedBatchDefinition> Create(CultureId cultureId, SortId sortId, GrowingConditions conditions,
        GrowingMethod method, EffectiveGrowingProfile profile, PlantCount targetPlants, PlanningChoices choices)
    {
        ArgumentNullException.ThrowIfNull(cultureId);
        ArgumentNullException.ThrowIfNull(sortId);
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(targetPlants);
        ArgumentNullException.ThrowIfNull(choices);
        if (!Enum.IsDefined(choices.SowingOrigin)) return Result.Failure<PlannedBatchDefinition>("Неизвестное происхождение выбранной даты");
        if (method is GrowingMethod.Transplant && profile.SeedlingAge is Option<SourcedValue<DayRange>>.None)
            return Result.Failure<PlannedBatchDefinition>("Не задан возраст рассады от всходов");
        if (method is GrowingMethod.DirectSow && choices.TransplantDate is Option<DateOnly>.Some)
            return Result.Failure<PlannedBatchDefinition>("Для прямого посева дата высадки неприменима");
        return Result.Success(new PlannedBatchDefinition(cultureId, sortId, conditions, method, profile, targetPlants, choices));
    }
}
