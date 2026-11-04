using System;
using System.Collections.Generic;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;

namespace Bonfire.Models.Planning;

public sealed partial class HarvestPlanningFields
{
    private Option<decimal> _inheritedPerPlant = Option.None<decimal>();
    private Option<decimal> _inheritedPerM2 = Option.None<decimal>();
    public void ConfigureInheritance(Option<decimal> perPlant, Option<decimal> perM2)
    {
        _inheritedPerPlant = perPlant; _inheritedPerM2 = perM2;
        OnPropertyChanged(string.Empty);
    }

    public IReadOnlyDictionary<string, PlanningFieldRule> FieldRules => new Dictionary<string, PlanningFieldRule>
    {
        [nameof(Mode)] = PlanningFieldRule.Checked("Режим количества растений", true, Enum.IsDefined(Mode)
            ? Result.Success(true) : Result.Failure<bool>("Выберите режим количества растений")),
        [nameof(UseLayout)] = new("Считать вместимость по прямоугольнику (метры)", RequiresLayout,
            !RequiresLayout || UseLayout ? Result.Success(true) : Result.Failure<bool>("Укажите размеры и схему грядки: включите расчёт по прямоугольнику")),
        [nameof(Length)] = GeometryRule("Длина, м", Length, true),
        [nameof(Width)] = GeometryRule("Ширина, м", Width, true),
        [nameof(PlantStep)] = GeometryRule("Шаг растения, м", PlantStep, true),
        [nameof(RowStep)] = GeometryRule("Шаг ряда, м", RowStep, true),
        [nameof(StartMargin)] = GeometryRule("Отступ начала, м", StartMargin, false),
        [nameof(EndMargin)] = GeometryRule("Отступ конца, м", EndMargin, false),
        [nameof(LeftMargin)] = GeometryRule("Отступ слева, м", LeftMargin, false),
        [nameof(RightMargin)] = GeometryRule("Отступ справа, м", RightMargin, false),
        [nameof(TargetKg)] = NumberRule("Цель урожая, кг", TargetKg, Mode is HarvestPlanningMode.TargetPerPlant or HarvestPlanningMode.TargetPerM2, true, Option.None<decimal>()),
        [nameof(PerPlantKg)] = NumberRule("Норма, кг/растение", PerPlantKg, Mode == HarvestPlanningMode.TargetPerPlant, Mode == HarvestPlanningMode.TargetPerPlant, _inheritedPerPlant),
        [nameof(PerM2Kg)] = NumberRule("Норма, кг/м²", PerM2Kg, Mode == HarvestPlanningMode.TargetPerM2, Mode == HarvestPlanningMode.TargetPerM2, _inheritedPerM2)
    };
    private bool RequiresLayout => Mode is HarvestPlanningMode.BedCapacity or HarvestPlanningMode.TargetPerM2;
    public Result<bool> ValidateRequiredFields() => PlanningFieldRule.ValidateAll(FieldRules.Values);

    private PlanningFieldRule GeometryRule(string name, string text, bool positive) => UseLayout
        ? NumberRule(name, text, true, positive, Option.None<decimal>())
        : new(name, false, Result.Success(true));

    private static PlanningFieldRule NumberRule(string name, string text, bool required, bool positive, Option<decimal> inherited)
        => PlanningFieldRule.Optional(name, required, PlanningInputParser.Decimal(text, name).Bind(value =>
        {
            var effective = value.Match(Option.Some, () => inherited);
            return effective.Match(number => number < 0 || positive && number == 0, () => false)
                ? Result.Failure<Option<decimal>>(name + (positive ? ": требуется положительное число" : ": требуется неотрицательное число"))
                : Result.Success(effective);
        }));
}
