using System;
using System.Globalization;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Models.Planning;

namespace Bonfire.Models.Mappers;

public static class PlanningMapper
{
    public static PlannedBatchEditorDraft Draft(int regionId, StoredPlannedBatchFromViewModel stored)
    {
        var document = stored.Document;
        var batch = document.Batch;
        var fields = Fields(document.Settings.Overrides, new PlanningExtras());
        if (document.Settings.Extras is Option<EffectivePlanningExtras>.Some extra)
            fields = fields with
            {
                GermPercentText = BatchValue(extra.Value.GerminationPercentage, value => Number(value.Value)),
                LossPercentText = BatchValue(extra.Value.LossPercentage, value => Number(value.Value)),
                LunarMode = extra.Value.LunarGroup.Match(value => value.Source == ParameterSource.Batch ? (int)value.Value : -1, () => -1)
            };
        var values = new PlannedBatchDraftValues
        {
            Choice = new(batch.CultureId.Value, document.References.CultureName, batch.SortId.Value, document.References.SortName),
            DirectSow = batch.Method is GrowingMethod.DirectSow,
            Location = batch.Conditions.Location, Placement = batch.Conditions.Placement,
            TargetPlantsText = batch.TargetPlants.Value.ToString(CultureInfo.InvariantCulture),
            SearchLimitText = document.Settings.SearchLimitDays.ToString(CultureInfo.InvariantCulture),
            SowingOrigin = batch.Choices.SowingOrigin,
            ManualSeedCountText = batch.Choices.ManualSeedCount.Match(value => value.Value.ToString(CultureInfo.InvariantCulture), () => ""),
            ChosenSowingText = PlanningInputParser.Format(batch.Choices.SowingDate),
            ChosenTransplantText = PlanningInputParser.Format(batch.Choices.TransplantDate), Profile = fields
        };
        values = batch.Method switch
        {
            GrowingMethod.Transplant method => values with { TransplantStartText = PlanningInputParser.Format(method.TransplantWindow.Start), TransplantEndText = PlanningInputParser.Format(method.TransplantWindow.End) },
            GrowingMethod.DirectSow method => values with { DirectStartText = PlanningInputParser.Format(method.SowingWindow.Start), DirectEndText = PlanningInputParser.Format(method.SowingWindow.End) },
            _ => throw new InvalidOperationException("Неизвестный способ выращивания")
        };
        return new(regionId, values, Option.Some(stored.Id));
    }

    public static GrowingProfileFieldValues Fields(GrowingProfileParameters parameters, PlanningExtras extras)
        => Fields(new PlanningOverrides { Germination = parameters.Germination, SeedlingAge = parameters.SeedlingAge,
            Vegetation = parameters.Vegetation, PrickingOut = extras.PrickingOut, Hardening = extras.Hardening }, extras);

    public static GrowingProfileFieldValues Fields(PlanningOverrides overrides, PlanningExtras extras) => new()
    {
        GermMinText = Bound(overrides.Germination, false), GermMaxText = Bound(overrides.Germination, true),
        AgeMinText = Bound(overrides.SeedlingAge, false), AgeMaxText = Bound(overrides.SeedlingAge, true),
        VegetationMinText = Bound(overrides.Vegetation, false), VegetationMaxText = Bound(overrides.Vegetation, true),
        PrickingMode = Mode(overrides.PrickingOut), HardeningMode = Mode(overrides.Hardening),
        PrickingMinText = OperationBound(overrides.PrickingOut, false), PrickingMaxText = OperationBound(overrides.PrickingOut, true),
        HardeningMinText = OperationBound(overrides.Hardening, false), HardeningMaxText = OperationBound(overrides.Hardening, true),
        GermPercentText = extras.GerminationPercentage.Match(value => Number(value.Value), () => ""),
        LossPercentText = extras.LossPercentage.Match(value => Number(value.Value), () => ""),
        LunarMode = extras.LunarGroup.Match(value => (int)value, () => -1),
        SoilTemperatureText = extras.ReferenceSoilTemperatureC.Match(Number, () => "")
    };

    private static string BatchValue<T>(Option<SourcedValue<T>> source, Func<T, string> format)
        => source.Match(value => value.Source == ParameterSource.Batch ? format(value.Value) : "", () => "");
    private static string Bound(Option<DayRange> range, bool upper) => range.Match(value => (upper ? value.Max : value.Min).ToString(CultureInfo.InvariantCulture), () => "");
    private static string OperationBound(OperationOverride<DayRange> operation, bool upper) => operation is OperationOverride<DayRange>.Enabled enabled
        ? (upper ? enabled.Settings.Max : enabled.Settings.Min).ToString(CultureInfo.InvariantCulture) : "";
    private static int Mode(OperationOverride<DayRange> operation) => operation switch
    {
        OperationOverride<DayRange>.Inherit => 0, OperationOverride<DayRange>.Disabled => 1, OperationOverride<DayRange>.Enabled => 2,
        _ => throw new InvalidOperationException("Неизвестный режим операции")
    };
    private static string Number(decimal value) => value.ToString(CultureInfo.CurrentCulture);
}
