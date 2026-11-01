using System.Collections.Generic;
using System.Linq;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;

namespace Bonfire.Models.Planning;

public sealed partial class PlannedBatchEditorDraft
{
    public IReadOnlyDictionary<string, PlanningFieldRule> FieldRules => new Dictionary<string, PlanningFieldRule>
    {
        [nameof(Choice)] = PlanningFieldRule.Checked("Культура / сорт", true, Result.From(Choice, Choice is null ? "Выберите культуру и сорт" : null)),
        [nameof(Location)] = PlanningFieldRule.Checked("Место выращивания", true, GrowingConditions.Create(Location, Placement)),
        [nameof(Placement)] = PlanningFieldRule.Checked("Размещение", true, GrowingConditions.Create(Location, Placement)),
        [nameof(TargetPlantsText)] = PlanningFieldRule.Checked("Количество растений", Harvest.IsManualPlants,
            Harvest.IsManualPlants ? PlanningInputParser.Count(TargetPlantsText, "Количество растений").Bind(_ => Result.Success(true)) : Result.Success(true)),
        [nameof(TransplantStartText)] = PeriodRule("Начало периода", TransplantStartText, TransplantEndText, true, !DirectSow),
        [nameof(TransplantEndText)] = PeriodRule("Окончание периода", TransplantStartText, TransplantEndText, false, !DirectSow),
        [nameof(DirectStartText)] = PeriodRule("Начало периода", DirectStartText, DirectEndText, true, DirectSow),
        [nameof(DirectEndText)] = PeriodRule("Окончание периода", DirectStartText, DirectEndText, false, DirectSow)
    };

    public Result<bool> ValidateFields(params string[] names) => names.Aggregate(Result.Success(true),
        (result, name) => result.Bind(_ => FieldRules[name].Validation));

    private Result<bool> ValidateRequiredFields() => PlanningFieldRule.ValidateAll(FieldRules.Values.Concat(Profile.FieldRules));

    private static PlanningFieldRule PeriodRule(string name, string start, string end, bool isStart, bool active)
    {
        var error = active ? WindowError(start, end, isStart) : "";
        return new(name, active, error.Length == 0 ? Result.Success(true) : Result.Failure<bool>(error));
    }
}
