using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models.Planning;

public sealed record GrowingProfileFieldValues
{
    public string GermMinText { get; init; } = "";
    public string GermMaxText { get; init; } = "";
    public string AgeMinText { get; init; } = "";
    public string AgeMaxText { get; init; } = "";
    public string VegetationMinText { get; init; } = "";
    public string VegetationMaxText { get; init; } = "";
    public string PrickingMinText { get; init; } = "";
    public string PrickingMaxText { get; init; } = "";
    public string HardeningMinText { get; init; } = "";
    public string HardeningMaxText { get; init; } = "";
    public string GermPercentText { get; init; } = "";
    public string LossPercentText { get; init; } = "";
    public string SoilTemperatureText { get; init; } = "";
    public string YieldPerPlantText { get; init; } = "";
    public string YieldPerM2Text { get; init; } = "";
    public LifeCycleFieldValues LifeCycle { get; init; } = new();
    public int PrickingMode { get; init; }
    public int HardeningMode { get; init; }
    public int LunarMode { get; init; } = -1;
}

public sealed class GrowingProfileFields : ViewModel, IDataErrorInfo
{
    public GrowingProfileFields(GrowingProfileFieldValues values, bool showRanges = true,
        bool showSoilTemperature = false)
    {
        (Values, ShowRanges, ShowSoilTemperature) = (values, showRanges, showSoilTemperature);
        LifeCycle = new LifeCycleFields(values.LifeCycle);
        LifeCycle.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(LifeCycleFields.Values))
                Update(Values with { LifeCycle = LifeCycle.Values }, nameof(LifeCycle));
        };
    }

    public GrowingProfileFieldValues Values { get; private set; }
    public LifeCycleFields LifeCycle { get; }
    public bool ShowRanges { get; }
    public bool ShowSoilTemperature { get; }

    public bool ShowSeedlingFields
    {
        get;
        set
        {
            if (Set(ref field, value)) OnPropertyChanged(string.Empty);
        }
    } = true;

    public string GermMinText
    {
        get => Values.GermMinText;
        set => Update(Values with { GermMinText = value });
    }

    public string GermMaxText
    {
        get => Values.GermMaxText;
        set => Update(Values with { GermMaxText = value });
    }

    public string AgeMinText
    {
        get => Values.AgeMinText;
        set => Update(Values with { AgeMinText = value });
    }

    public string AgeMaxText
    {
        get => Values.AgeMaxText;
        set => Update(Values with { AgeMaxText = value });
    }

    public string VegetationMinText
    {
        get => Values.VegetationMinText;
        set => Update(Values with { VegetationMinText = value });
    }

    public string VegetationMaxText
    {
        get => Values.VegetationMaxText;
        set => Update(Values with { VegetationMaxText = value });
    }

    public string PrickingMinText
    {
        get => Values.PrickingMinText;
        set => Update(Values with { PrickingMinText = value });
    }

    public string PrickingMaxText
    {
        get => Values.PrickingMaxText;
        set => Update(Values with { PrickingMaxText = value });
    }

    public string HardeningMinText
    {
        get => Values.HardeningMinText;
        set => Update(Values with { HardeningMinText = value });
    }

    public string HardeningMaxText
    {
        get => Values.HardeningMaxText;
        set => Update(Values with { HardeningMaxText = value });
    }

    public string GermPercentText
    {
        get => Values.GermPercentText;
        set => Update(Values with { GermPercentText = value });
    }

    public string LossPercentText
    {
        get => Values.LossPercentText;
        set => Update(Values with { LossPercentText = value });
    }

    public string SoilTemperatureText
    {
        get => Values.SoilTemperatureText;
        set => Update(Values with { SoilTemperatureText = value });
    }

    public string YieldPerPlantText
    {
        get => Values.YieldPerPlantText;
        set => Update(Values with { YieldPerPlantText = value });
    }

    public string YieldPerM2Text
    {
        get => Values.YieldPerM2Text;
        set => Update(Values with { YieldPerM2Text = value });
    }

    public int PrickingMode
    {
        get => Values.PrickingMode;
        set => Update(Values with { PrickingMode = value });
    }

    public int HardeningMode
    {
        get => Values.HardeningMode;
        set => Update(Values with { HardeningMode = value });
    }

    public int LunarMode
    {
        get => Values.LunarMode;
        set => Update(Values with { LunarMode = value });
    }

    public bool PrickingEnabled => PrickingMode == 2;
    public bool HardeningEnabled => HardeningMode == 2;

    public string Error => PlanningInputParser.Error(BuildOverrides(!ShowSeedlingFields)) is { Length: > 0 } error
        ? error
        : PlanningInputParser.Error(BuildExtras(!ShowSeedlingFields));

    public string this[string property] => property switch
    {
        nameof(GermMinText) or nameof(GermMaxText) => PlanningInputParser.Error(
            PlanningInputParser.Range(GermMinText, GermMaxText, "Прорастание")),
        nameof(AgeMinText) or nameof(AgeMaxText) when ShowSeedlingFields => PlanningInputParser.Error(
            PlanningInputParser.Range(AgeMinText, AgeMaxText, "Возраст")),
        nameof(VegetationMinText) or nameof(VegetationMaxText) => PlanningInputParser.Error(
            PlanningInputParser.Range(VegetationMinText, VegetationMaxText, "Вегетация")),
        nameof(GermPercentText) => PlanningInputParser.Error(PlanningInputParser.Decimal(GermPercentText, "Всхожесть")
            .Bind(x => Optional(x, GerminationPercentage.Create))),
        nameof(LossPercentText) => PlanningInputParser.Error(PlanningInputParser.Decimal(LossPercentText, "Потери")
            .Bind(x => Optional(x, LossPercentage.Create))),
        nameof(PrickingMinText) or nameof(PrickingMaxText) when ShowSeedlingFields => PlanningInputParser.Error(
            Operation(PrickingMode, PrickingMinText, PrickingMaxText, "Пикировка")),
        nameof(HardeningMinText) or nameof(HardeningMaxText) when ShowSeedlingFields => PlanningInputParser.Error(
            Operation(HardeningMode, HardeningMinText, HardeningMaxText, "Закаливание")),
        nameof(SoilTemperatureText) => PlanningInputParser.Error(
            PlanningInputParser.Decimal(SoilTemperatureText, "Температура")),
        nameof(YieldPerPlantText) or nameof(YieldPerM2Text) => PlanningInputParser.Error(
            BuildExtras(!ShowSeedlingFields)),
        _ => ""
    };

    public Result<PlanningOverrides> BuildOverrides(bool directSow)
        => PlanningInputParser.Range(GermMinText, GermMaxText, "Прорастание").Bind(g =>
            (directSow
                ? Result.Success(Option.None<DayRange>())
                : PlanningInputParser.Range(AgeMinText, AgeMaxText, "Возраст рассады"))
            .Bind(a => PlanningInputParser.Range(VegetationMinText, VegetationMaxText, "Вегетация от всходов")
                .Bind(v => Operation(directSow ? 0 : PrickingMode, PrickingMinText, PrickingMaxText, "Пикировка")
                    .Bind(p =>
                        Operation(directSow ? 0 : HardeningMode, HardeningMinText, HardeningMaxText, "Закаливание")
                            .Bind(h => Result.Success(new PlanningOverrides
                            {
                                Germination = g, SeedlingAge = a, Vegetation = v, PrickingOut = p, Hardening = h
                            }))))));

    public Result<PlanningExtras> BuildExtras(bool ignoreSeedling = false)
        => PlanningInputParser.Decimal(GermPercentText, "Всхожесть").Bind(g => Optional(g, GerminationPercentage.Create)
                .Bind(gp =>
                    PlanningInputParser.Decimal(LossPercentText, "Потери").Bind(l => Optional(l, LossPercentage.Create)
                        .Bind(lp =>
                            Operation(ignoreSeedling ? 0 : PrickingMode, PrickingMinText, PrickingMaxText, "Пикировка")
                                .Bind(p =>
                                    Operation(ignoreSeedling ? 0 : HardeningMode, HardeningMinText, HardeningMaxText,
                                        "Закаливание").Bind(h =>
                                        PlanningInputParser.Decimal(SoilTemperatureText, "Справочная температура")
                                            .Bind(t =>
                                                Lunar().Bind(moon => Result.Success(new PlanningExtras
                                                {
                                                    GerminationPercentage = gp, LossPercentage = lp, PrickingOut = p,
                                                    Hardening = h,
                                                    LunarGroup = moon, ReferenceSoilTemperatureC = t
                                                }))))))))).Bind(extras => PlanningInputParser
                .Decimal(YieldPerPlantText, "кг/растение").Bind(plant =>
                    PlanningInputParser.Decimal(YieldPerM2Text, "кг/м²").Bind(area =>
                        plant.Match(x => x < 0, () => false) || area.Match(x => x < 0, () => false)
                            ? Result.Failure<PlanningExtras>("Нормы урожайности не могут быть отрицательными")
                            : Result.Success(extras with { YieldPerPlantKg = plant, YieldPerM2Kg = area }))))
            .Bind(extras => LifeCycle.Build().Bind(life => Result.Success(extras with { LifeCycle = life })));

    private Result<Option<LunarGroup>> Lunar() => LunarMode == -1 ? Result.Success(Option.None<LunarGroup>())
        : Enum.IsDefined((LunarGroup)LunarMode) ? Result.Success(Option.Some((LunarGroup)LunarMode))
        : Result.Failure<Option<LunarGroup>>("Выберите лунную группу");

    private static Result<Option<T>> Optional<T>(Option<decimal> value, Func<decimal, Result<T>> factory)
        => value.Match(x => factory(x).Bind(result => Result.Success(Option.Some(result))),
            () => Result.Success(Option.None<T>()));

    private static Result<OperationOverride<DayRange>> Operation(int mode, string min, string max, string name) =>
        mode switch
        {
            0 => Result.Success<OperationOverride<DayRange>>(new OperationOverride<DayRange>.Inherit()),
            1 => Result.Success<OperationOverride<DayRange>>(new OperationOverride<DayRange>.Disabled()),
            2 => PlanningInputParser.Range(min, max, name).Bind(range => range.Match(
                days => Result.Success<OperationOverride<DayRange>>(new OperationOverride<DayRange>.Enabled(days)),
                () => Result.Failure<OperationOverride<DayRange>>(name + ": задайте интервал"))),
            _ => Result.Failure<OperationOverride<DayRange>>("Неизвестный режим операции")
        };

    private void Update(GrowingProfileFieldValues values, [CallerMemberName] string? property = null)
    {
        if (Values == values) return;
        Values = values;
        OnPropertyChanged(nameof(Values));
        OnPropertyChanged(property);
        OnPropertyChanged(string.Empty);
    }
}