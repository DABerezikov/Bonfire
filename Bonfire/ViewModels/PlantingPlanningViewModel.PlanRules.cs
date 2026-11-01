using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Models.Planning;

namespace Bonfire.ViewModels;

public sealed partial class PlantingPlanningViewModel : IDataErrorInfo
{
    public IReadOnlyDictionary<string, PlanningFieldRule> NewPlanFieldRules => new Dictionary<string, PlanningFieldRule>
    {
        [nameof(NewPlanName)] = PlanningFieldRule.Checked("Название", true, LibraryName.Create(NewPlanName)),
        [nameof(NewPlanYear)] = PlanningFieldRule.Checked("Год сезона", true, int.TryParse(NewPlanYear, out var year)
            ? SeasonalPlanDefinition.ValidateYear(year) : Result.Failure<int>("Год сезона: введите целое число")),
        [nameof(NewPlanRegion)] = PlanningFieldRule.Checked("Регион", true,
            Result.From(NewPlanRegion, NewPlanRegion is null ? "Выберите регион" : null))
    };
    string IDataErrorInfo.Error => PlanningInputParser.Error(PlanningFieldRule.ValidateAll(NewPlanFieldRules.Values));
    string IDataErrorInfo.this[string property] => NewPlanFieldRules.TryGetValue(property, out var rule) ? rule.Error : "";

    private void RefreshPlanRules()
    {
        OnPropertyChanged(nameof(NewPlanFieldRules));
        CommandManager.InvalidateRequerySuggested();
    }
}
