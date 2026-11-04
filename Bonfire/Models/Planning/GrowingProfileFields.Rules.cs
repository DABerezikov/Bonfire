using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Seeds;
using System.Collections.Generic;

namespace Bonfire.Models.Planning;

public sealed partial class GrowingProfileFields
{
    private GrowingProfileParameters _inheritedCulture = new();
    private SortCharacteristicsValues _inheritedSort = new();
    private Option<GerminationPercentage> _inheritedGermination = Option.None<GerminationPercentage>();
    private Option<LossPercentage> _inheritedLoss = Option.None<LossPercentage>();

    public void ConfigureInheritance(GrowingProfileParameters culture, SortCharacteristicsValues sort,
        Option<GerminationPercentage> germination, Option<LossPercentage> losses)
    {
        _inheritedCulture = culture; _inheritedSort = sort;
        _inheritedGermination = germination; _inheritedLoss = losses;
        OnPropertyChanged(string.Empty);
    }

    public PlanningFieldRule GerminationRule => RangeRule("Прорастание", GermMinText, GermMaxText, IsPlanningBatch,
        new(_inheritedSort.MinGerminationTime, _inheritedSort.MaxGerminationTime), _inheritedCulture.Germination);
    public PlanningFieldRule SeedlingAgeRule => ShowSeedlingFields
        ? RangeRule("Возраст от всходов", AgeMinText, AgeMaxText, IsPlanningBatch,
            new(_inheritedSort.AgeOfSeedlings, _inheritedSort.MaxAgeOfSeedlings), _inheritedCulture.SeedlingAge)
        : new("Возраст от всходов", false, Result.Success(true));
    public PlanningFieldRule GerminationPercentRule => PlanningFieldRule.Optional("Всхожесть, %", IsPlanningBatch && RequireSeedPercentages,
        PlanningInputParser.Decimal(GermPercentText, "Всхожесть").Bind(value => Optional(value, GerminationPercentage.Create))
            .Bind(value => Result.Success(value.Match(Option.Some, () => _inheritedGermination))));
    public PlanningFieldRule LossPercentRule => PlanningFieldRule.Optional("Потери после всходов, %", IsPlanningBatch && RequireSeedPercentages,
        PlanningInputParser.Decimal(LossPercentText, "Потери").Bind(value => Optional(value, LossPercentage.Create))
            .Bind(value => Result.Success(value.Match(Option.Some, () => _inheritedLoss))));
    public PlanningFieldRule PrickingRangeRule => PlanningFieldRule.Checked("Дней после всходов", ShowSeedlingFields && PrickingEnabled,
        Operation(ShowSeedlingFields ? PrickingMode : 0, PrickingMinText, PrickingMaxText, "Пикировка"));
    public PlanningFieldRule HardeningRangeRule => PlanningFieldRule.Checked("Длительность, дней", ShowSeedlingFields && HardeningEnabled,
        Operation(ShowSeedlingFields ? HardeningMode : 0, HardeningMinText, HardeningMaxText, "Закаливание"));
    public IReadOnlyList<PlanningFieldRule> FieldRules =>
        [GerminationRule, SeedlingAgeRule, GerminationPercentRule, LossPercentRule, PrickingRangeRule, HardeningRangeRule];

    public Result<bool> ValidateRequiredFields() => PlanningFieldRule.ValidateAll(FieldRules);

    private static PlanningFieldRule RangeRule(string name, string min, string max, bool required,
        RangeInput sort, Option<DayRange> culture)
    {
        var parsed = PlanningInputParser.Range(min, max, name);
        var validation = required
            ? parsed.Bind(value => PlanningParameters.ResolveRange(name, value, sort,
                    new(culture.Map(x => x.Min), culture.Map(x => x.Max))).Bind(_ => Result.Success(true)))
            : parsed.Bind(_ => Result.Success(true));
        return new(name, required, validation);
    }
}
