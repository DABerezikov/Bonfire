using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models.Planning;

public sealed record HarvestPlanningFieldValues
{
    public HarvestPlanningMode Mode { get; init; }
    public bool UseLayout { get; init; }
    public bool RowsAcross { get; init; }
    public string Length { get; init; } = "";
    public string Width { get; init; } = "";
    public string PlantStep { get; init; } = "0,4";
    public string RowStep { get; init; } = "0,6";
    public string StartMargin { get; init; } = "0,2";
    public string EndMargin { get; init; } = "0,2";
    public string LeftMargin { get; init; } = "0,3";
    public string RightMargin { get; init; } = "0,3";
    public string PerPlantKg { get; init; } = "";
    public string PerM2Kg { get; init; } = "";
    public string YieldAreaM2 { get; init; } = "";
    public string TargetKg { get; init; } = "";
    public int? BedId { get; init; }
    public string? BedName { get; init; }
}

public sealed partial class HarvestPlanningFields(HarvestPlanningFieldValues values) : ViewModel, IDataErrorInfo
{
    public HarvestPlanningFieldValues Values { get; private set; } = values;

    public HarvestPlanningMode Mode
    {
        get => Values.Mode;
        set => Update(Values with { Mode = value });
    }

    public bool IsManualPlants => Mode == HarvestPlanningMode.ManualPlants;
    public string ModeHint => Mode switch
    {
        HarvestPlanningMode.BedCapacity => "Обязательны размеры и схема грядки: включите расчёт по прямоугольнику или возьмите размеры выбранной грядки.",
        HarvestPlanningMode.TargetPerPlant => "Обязательны цель в кг и положительная норма кг/растение (здесь либо в сорте/культуре).",
        HarvestPlanningMode.TargetPerM2 => "Обязательны цель в кг, норма кг/м² (здесь либо в сорте/культуре), размеры и схема грядки.",
        _ => "Обязательно количество растений. Грядка и нормы урожайности необязательны."
    };
    public string TargetLabel => FieldRules[nameof(TargetKg)].Caption;
    public string PerPlantLabel => FieldRules[nameof(PerPlantKg)].Caption;
    public string PerM2Label => FieldRules[nameof(PerM2Kg)].Caption;

    public bool UseLayout
    {
        get => Values.UseLayout;
        set => Update(Values with { UseLayout = value });
    }

    public bool RowsAcross
    {
        get => Values.RowsAcross;
        set => Update(Values with { RowsAcross = value });
    }

    public string Length
    {
        get => Values.Length;
        set => Update(Values with { Length = value, BedId = null, BedName = null });
    }

    public string Width
    {
        get => Values.Width;
        set => Update(Values with { Width = value, BedId = null, BedName = null });
    }

    public string PlantStep
    {
        get => Values.PlantStep;
        set => Update(Values with { PlantStep = value });
    }

    public string RowStep
    {
        get => Values.RowStep;
        set => Update(Values with { RowStep = value });
    }

    public string StartMargin
    {
        get => Values.StartMargin;
        set => Update(Values with { StartMargin = value });
    }

    public string EndMargin
    {
        get => Values.EndMargin;
        set => Update(Values with { EndMargin = value });
    }

    public string LeftMargin
    {
        get => Values.LeftMargin;
        set => Update(Values with { LeftMargin = value });
    }

    public string RightMargin
    {
        get => Values.RightMargin;
        set => Update(Values with { RightMargin = value });
    }

    public string PerPlantKg
    {
        get => Values.PerPlantKg;
        set => Update(Values with { PerPlantKg = value });
    }

    public string PerM2Kg
    {
        get => Values.PerM2Kg;
        set => Update(Values with { PerM2Kg = value });
    }

    public string YieldAreaM2
    {
        get => Values.YieldAreaM2;
        set => Update(Values with { YieldAreaM2 = value });
    }

    public string TargetKg
    {
        get => Values.TargetKg;
        set => Update(Values with { TargetKg = value });
    }

    public string BedSource => Values.BedName is { } name ? $"Снимок размеров: {name}" : "Размеры вручную";

    public string Error => PlanningInputParser.Error(Build());
    public string this[string property] => FieldRules.TryGetValue(property, out var rule) ? rule.Error : Error;

    public void ApplyBed(int id, string name, decimal length, decimal width)
        => Update(Values with
        {
            UseLayout = true, BedId = id, BedName = name, Length = Number(length), Width = Number(width)
        });

    public void HalfStepMargins()
    {
        PlanningInputParser.Decimal(PlantStep, "Шаг растения").Bind(p => PlanningInputParser
                .Decimal(RowStep, "Шаг ряда")
                .Bind(r => p is Option<decimal>.Some plant && r is Option<decimal>.Some row && plant.Value > 0 &&
                           row.Value > 0
                    ? Result.Success((Plant: plant.Value, Row: row.Value))
                    : Result.Failure<(decimal Plant, decimal Row)>("Задайте положительные шаги")))
            .Match(steps =>
            {
                Update(Values with
                {
                    StartMargin = Number(steps.Plant / 2), EndMargin = Number(steps.Plant / 2),
                    LeftMargin = Number(steps.Row / 2), RightMargin = Number(steps.Row / 2)
                });
                return 0;
            }, _ => 0);
    }

    public Result<HarvestPlanningInput> Build() => ValidateRequiredFields().Bind(_ => BuildChecked());

    private Result<HarvestPlanningInput> BuildChecked()
    {
        var errors = new List<string>();

        Option<decimal> Parse(string text, string name, bool required = false)
            => PlanningInputParser.Decimal(text, name).Match(value =>
            {
                if (value.Match(x => x < 0, () => required)) errors.Add(name + ": требуется неотрицательное число");
                return value;
            }, error =>
            {
                errors.Add(error);
                return Option.None<decimal>();
            });

        decimal Geometry(string text, string name) => Parse(text, name, true).Match(x => x, () => 0m);
        var layout = UseLayout
            ? Option.Some(new BedLayout(Geometry(Length, "Длина"), Geometry(Width, "Ширина"),
                Geometry(PlantStep, "Шаг растения"), Geometry(RowStep, "Шаг ряда"),
                Geometry(StartMargin, "Начальный отступ"),
                Geometry(EndMargin, "Конечный отступ"), Geometry(LeftMargin, "Левый отступ"),
                Geometry(RightMargin, "Правый отступ"), RowsAcross))
            : Option.None<BedLayout>();
        var input = new HarvestPlanningInput
        {
            Mode = Mode, Layout = layout, BedId = Values.BedId.ToOption(), BedName = Values.BedName.ToOption(),
            PerPlantKg = Parse(PerPlantKg, "кг/растение"), PerM2Kg = Parse(PerM2Kg, "кг/м²"),
            TargetKg = Parse(TargetKg, "Цель кг"), YieldAreaM2 = Parse(YieldAreaM2, "Площадь урожая")
        };
        if (errors.Count > 0) return Result.Failure<HarvestPlanningInput>(errors[0]);
        return layout.Match(x => BedCapacity.Calculate(x).Bind(_ => Result.Success(input)),
            () => Result.Success(input));
    }

    public static HarvestPlanningFieldValues From(HarvestPlanningInput input)
    {
        var values = new HarvestPlanningFieldValues
        {
            Mode = input.Mode,
            PerPlantKg = input.PerPlantSource == ParameterSource.Batch ? Format(input.PerPlantKg) : "",
            PerM2Kg = input.PerM2Source == ParameterSource.Batch ? Format(input.PerM2Kg) : "",
            TargetKg = Format(input.TargetKg), YieldAreaM2 = Format(input.YieldAreaM2),
            BedId = input.BedId.ToNullable(), BedName = input.BedName.ToNullable()
        };
        return input.Layout.Match(x => values with
        {
            UseLayout = true, Length = Number(x.Length), Width = Number(x.Width), PlantStep = Number(x.PlantStep),
            RowStep = Number(x.RowStep), StartMargin = Number(x.StartMargin), EndMargin = Number(x.EndMargin),
            LeftMargin = Number(x.LeftMargin),
            RightMargin = Number(x.RightMargin), RowsAcross = x.RowsAcross
        }, () => values);
    }

    private static string Number(decimal value) => value.ToString(CultureInfo.CurrentCulture);
    private static string Format(Option<decimal> value) => value.Match(Number, () => "");

    private void Update(HarvestPlanningFieldValues values, [CallerMemberName] string? name = null)
    {
        if (Values == values) return;
        Values = values;
        OnPropertyChanged(nameof(Values));
        OnPropertyChanged(name);
        OnPropertyChanged(string.Empty);
    }
}
