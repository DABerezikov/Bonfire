using System.Collections.Generic;
using Bonfire.Domain.Functional;

namespace Bonfire.Models.Planning;

public sealed partial class LifeCycleFields
{
    public IReadOnlyDictionary<string, PlanningFieldRule> FieldRules => new Dictionary<string, PlanningFieldRule>
    {
        [nameof(FirstFlower)] = SeasonRule("Первый сезон цветения", FirstFlower),
        [nameof(FirstHarvest)] = SeasonRule("Первый сезон урожая", FirstHarvest),
        [nameof(FlowerStartMin)] = RangeRule("Начало цветения", FlowerStartMin, FlowerStartMax),
        [nameof(FlowerEndMin)] = RangeRule("Окончание цветения", FlowerEndMin, FlowerEndMax),
        [nameof(FruitStartMin)] = RangeRule("Начало плодоношения", FruitStartMin, FruitStartMax),
        [nameof(FruitEndMin)] = RangeRule("Окончание плодоношения", FruitEndMin, FruitEndMax),
        [nameof(FlowerAnnualStart)] = AnnualRule("Цветение, с / по", FlowerAnnualStart, FlowerAnnualEnd, FlowerNextYear),
        [nameof(FruitAnnualStart)] = AnnualRule("Плодоношение, с / по", FruitAnnualStart, FruitAnnualEnd, FruitNextYear)
    };
    private PlanningFieldRule SeasonRule(string name, string text) => ShowDetails
        ? PlanningFieldRule.Checked(name, true, PlanningInputParser.Count(text, name))
        : new(name, false, Result.Success(true));
    private PlanningFieldRule RangeRule(string name, string min, string max) => ShowDetails
        ? PlanningFieldRule.Checked(name, min.Length > 0 || max.Length > 0, PlanningInputParser.Range(min, max, name))
        : new(name, false, Result.Success(true));
    private PlanningFieldRule AnnualRule(string name, string start, string end, bool nextYear) => ShowDetails
        ? PlanningFieldRule.Checked(name, start.Length > 0 || end.Length > 0, PlanningInputParser.Annual(start, end, nextYear))
        : new(name, false, Result.Success(true));
}
