using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models.Planning;

public sealed record PlannedBatchDraftValues
{
    public PlanningChoiceFromViewModel? Choice { get; init; }
    public bool DirectSow { get; init; }
    public GrowingLocation Location { get; init; }
    public PlantPlacement Placement { get; init; }
    public string TargetPlantsText { get; init; } = "20";
    public string SearchLimitText { get; init; } = "35";
    public string TransplantStartText { get; init; } = "";
    public string TransplantEndText { get; init; } = "";
    public string DirectStartText { get; init; } = "";
    public string DirectEndText { get; init; } = "";
    public string ChosenSowingText { get; init; } = "";
    public string ChosenTransplantText { get; init; } = "";
    public string ManualSeedCountText { get; init; } = "";
    public GrowingProfileFieldValues Profile { get; init; } = new();
    public HarvestPlanningFieldValues Harvest { get; init; } = new();
    public PlannedDateOrigin SowingOrigin { get; init; } = PlannedDateOrigin.Manual;
}

public sealed partial class PlannedBatchEditorDraft : ViewModel, IDataErrorInfo
{
    public PlannedBatchEditorDraft(int regionId) : this(regionId, new PlannedBatchDraftValues(), Option.None<int>())
    {
    }

    public PlannedBatchEditorDraft(int regionId, PlannedBatchDraftValues values, Option<int> batchId)
    {
        RegionId = regionId;
        BatchId = batchId;
        Values = Original = values;
        Harvest = new HarvestPlanningFields(values.Harvest);
        Harvest.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(HarvestPlanningFields.Values))
                Update(Values with { Harvest = Harvest.Values }, nameof(Harvest));
        };
        Profile = new GrowingProfileFields(values.Profile)
        {
            ShowSeedlingFields = !values.DirectSow, IsPlanningBatch = true,
            RequireSeedPercentages = values.ManualSeedCountText.Length == 0
        };
        Profile.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(GrowingProfileFields.Values))
                Update(Values with { Profile = Profile.Values }, nameof(Profile));
        };
    }

    public int RegionId { get; }
    public Option<int> BatchId { get; private set; }
    public PlannedBatchDraftValues Values { get; private set; }
    public PlannedBatchDraftValues Original { get; private set; }
    public GrowingProfileFields Profile { get; }
    public HarvestPlanningFields Harvest { get; }
    public long Revision { get; private set; }
    public bool IsDirty => Values != Original;
    public bool IsSeedling => !DirectSow;
    public string WindowLabel => DirectSow ? "Период прямого посева" : "Период высадки рассады";

    public PlanningChoiceFromViewModel? Choice
    {
        get => Values.Choice;
        set => Update(Values with { Choice = value });
    }

    public bool DirectSow
    {
        get => Values.DirectSow;
        set
        {
            Profile.ShowSeedlingFields = !value;
            Update(Values with { DirectSow = value });
        }
    }

    public GrowingLocation Location
    {
        get => Values.Location;
        set => Update(Values with { Location = value });
    }

    public PlantPlacement Placement
    {
        get => Values.Placement;
        set => Update(Values with { Placement = value });
    }

    public string TargetPlantsText
    {
        get => Values.TargetPlantsText;
        set => Update(Values with { TargetPlantsText = value });
    }

    public string SearchLimitText
    {
        get => Values.SearchLimitText;
        set => Update(Values with { SearchLimitText = value });
    }

    public string TransplantStartText
    {
        get => Values.TransplantStartText;
        set => Update(Values with { TransplantStartText = value });
    }

    public string TransplantEndText
    {
        get => Values.TransplantEndText;
        set => Update(Values with { TransplantEndText = value });
    }

    public string DirectStartText
    {
        get => Values.DirectStartText;
        set => Update(Values with { DirectStartText = value });
    }

    public string DirectEndText
    {
        get => Values.DirectEndText;
        set => Update(Values with { DirectEndText = value });
    }

    public string ChosenSowingText
    {
        get => Values.ChosenSowingText;
        set => Update(Values with { ChosenSowingText = value, SowingOrigin = PlannedDateOrigin.Manual });
    }

    public string ChosenTransplantText
    {
        get => Values.ChosenTransplantText;
        set => Update(Values with { ChosenTransplantText = value });
    }

    public string ManualSeedCountText
    {
        get => Values.ManualSeedCountText;
        set
        {
            Profile.RequireSeedPercentages = value.Length == 0;
            Update(Values with { ManualSeedCountText = value });
        }
    }

    public string Error => PlanningInputParser.Error(BuildInput());

    public string this[string property] => FieldRules.TryGetValue(property, out var rule) ? rule.Error : property switch
    {
        nameof(ManualSeedCountText) => PlanningInputParser.Error(
            PlanningInputParser.OptionalCount(ManualSeedCountText, "Семена")),
        nameof(ChosenSowingText) => PlanningInputParser.Error(
            PlanningInputParser.OptionalDate(ChosenSowingText, "Посев")),
        nameof(ChosenTransplantText) when !DirectSow => PlanningInputParser.Error(
            PlanningInputParser.OptionalDate(ChosenTransplantText, "Высадка")),
        nameof(SearchLimitText) => int.TryParse(SearchLimitText, out var limit) && limit >= 0
            ? ""
            : "Укажите неотрицательное число дней",
        _ => ""
    };

    private static string WindowError(string start, string end, bool isStart)
    {
        var own = PlanningInputParser.Date(isStart ? start : end, isStart ? "Начало периода" : "Окончание периода");
        return own.Match(_ => PlanningInputParser.Date(isStart ? end : start, "Другая граница").Match(
            _ => PlanningInputParser.Error(PlanningInputParser.Window(start, end)), _ => ""), error => error);
    }

    public Result<PlanningBatchInputFromViewModel> BuildInput() => ValidateRequiredFields().Bind(_ => BuildCheckedInput());

    private Result<PlanningBatchInputFromViewModel> BuildCheckedInput()
    {
        if (Choice is null) return Result.Failure<PlanningBatchInputFromViewModel>("Выберите культуру и сорт");
        if (!int.TryParse(SearchLimitText, out var limit) || limit < 0)
            return Result.Failure<PlanningBatchInputFromViewModel>(
                "Предел сдвига должен быть целым неотрицательным числом дней");
        var window = DirectSow
            ? PlanningInputParser.Window(DirectStartText, DirectEndText)
            : PlanningInputParser.Window(TransplantStartText, TransplantEndText);
        return Harvest.Build().Bind(harvest => GrowingConditions.Create(Location, Placement).Bind(conditions =>
            (Harvest.IsManualPlants
                ? PlanningInputParser.Count(TargetPlantsText, "Количество растений")
                : PlantCount.Create(1)).Bind(target =>
                window.Bind(dates => Profile.BuildOverrides(DirectSow).Bind(overrides =>
                    Profile.BuildExtras(DirectSow).Bind(extras =>
                        PlanningInputParser.OptionalCount(ManualSeedCountText, "Количество семян").Bind(seeds =>
                            PlanningInputParser.OptionalDate(ChosenSowingText, "Выбранный посев").Bind(sowing =>
                                (DirectSow
                                    ? Result.Success(Option.None<DateOnly>())
                                    : PlanningInputParser.OptionalDate(ChosenTransplantText, "Выбранная высадка"))
                                .Bind(transplant => Result.Success(new PlanningBatchInputFromViewModel(RegionId,
                                        Choice.CultureId, Choice.SortId,
                                        conditions,
                                        DirectSow
                                            ? new GrowingMethod.DirectSow(dates)
                                            : new GrowingMethod.Transplant(dates),
                                        target, overrides,
                                        new PlanningChoices(sowing, transplant, seeds)
                                            { SowingOrigin = Values.SowingOrigin }, extras.GerminationPercentage,
                                        extras.LossPercentage, extras.LunarGroup, limit)
                                    { Harvest = harvest, LifeCycleOverride = extras.LifeCycle }))))))))));
    }

    public void AcceptSaved(PlannedBatchDraftValues submitted, int batchId)
    {
        if (batchId <= 0) throw new ArgumentOutOfRangeException(nameof(batchId));
        Original = submitted;
        BatchId = Option.Some(batchId);
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(BatchId));
    }

    public void ChooseLunarDate(DateOnly date) => Update(Values with
    {
        ChosenSowingText = PlanningInputParser.Format(date), SowingOrigin = PlannedDateOrigin.LunarRecommendation
    }, nameof(ChosenSowingText));

    private void Update(PlannedBatchDraftValues values, [CallerMemberName] string? property = null)
    {
        if (Values == values) return;
        Values = values;
        Revision++;
        OnPropertyChanged(nameof(Values));
        OnPropertyChanged(property);
        OnPropertyChanged(string.Empty);
    }
}
