using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Libraries;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models.Planning;

namespace Bonfire.ViewModels;

public sealed partial class PlantingPlanningViewModel
{
    private int _editorStep;
    public int EditorStep
    {
        get => _editorStep;
        set
        {
            if (value < 0 || value > 4 || !CanOpenStep(value)) return;
            if (Set(ref _editorStep, value)) RefreshSteps();
        }
    }

    public bool DatesStepEnabled => CanOpenStep(1);
    public bool QuantityStepEnabled => CanOpenStep(2);
    public bool ParametersStepEnabled => CanOpenStep(3);
    public bool ResultStepEnabled => CanOpenStep(4);
    public bool IsResultStep => EditorStep == 4;
    public bool HasNextStep => EditorStep < 4;
    public bool EditorAvailable => !IsBusy;
    public string SaveGuidance => CanSave() ? "Сохранить рассчитанную партию в сезонный план"
        : !Draft.IsDirty ? "Нет изменений для сохранения."
        : Preview is null ? "Сначала выполните расчёт."
        : Preview.MissingInputs.Length > 0 ? CalculationGuidance : "Проверьте параметры и пересчитайте изменённый ввод.";
    public string StepTitle => $"Шаг {EditorStep + 1} из 5";
    public string SelectedSowingText => Draft.ChosenSowingText.Length == 0
        ? "День посева пока не выбран: выберите вариант ниже или задайте дату на шаге «Сроки»."
        : "Выбран посев: " + Draft.ChosenSowingText
          + (Draft.Values.SowingOrigin == PlannedDateOrigin.LunarRecommendation ? " (из лунных рекомендаций). " : " (вручную). ")
          + (Preview is null ? "Требуется пересчёт." : "Для записи в план нажмите «Сохранить партию».");
    public string StepGuidance => IsBusy ? "Выполняется операция…" : OpenedPlan is null
        ? "Сначала создайте или выберите сезонный план слева."
        : ValidateStep(EditorStep).Match(_ => EditorStep switch
        {
            0 => "Выберите способ посева и условия выращивания, затем нажмите «Далее».",
            1 => "Обе границы включены в период. Региональная справка помогает выбрать даты; рабочий интервал задаёте вы.",
            2 => "Выберите способ определения количества. Грядка необязательна при ручном количестве растений.",
            3 => "Пустые параметры наследуются из сорта и культуры. Нажмите «Далее», чтобы выполнить расчёт.",
            _ => "Проверьте прогноз. Для выбора ручной даты вернитесь к срокам; для исправления параметров — на предыдущий шаг."
        }, error => "Для продолжения: " + error);

    public string CalculationGuidance => IsBusy ? "Выполняется расчёт…" : OpenedPlan is null
        ? "Выберите сезонный план."
        : Draft.BuildInput().Match(_ => Preview is { MissingInputs.Length: > 0 } preview
            ? "Не хватает данных: " + string.Join("; ", preview.MissingInputs) + ". Заполните их на шаге «Параметры»."
            : Preview is null ? "Выполните расчёт перед сохранением." : "Расчёт готов. Сохраните партию, чтобы включить её в план.",
            error => "Расчёт недоступен: " + error);

    public ICommand PreviousStepCommand => field ??= new LambdaCommand(() => EditorStep--, () => !IsBusy && EditorStep > 0);
    public ICommand NextStepCommand => field ??= new LambdaCommandAsync(NextStepAsync,
        () => !IsBusy && EditorStep < 4 && CanOpenStep(EditorStep + 1));

    private async Task NextStepAsync()
    {
        if (!CanOpenStep(EditorStep + 1)) return;
        EditorStep++;
        if (IsResultStep) await CalculateAsync();
    }

    private bool CanOpenStep(int step)
    {
        if (step == 0) return true;
        if (OpenedPlan is null) return false;
        for (var previous = 0; previous < step; previous++)
            if (ValidateStep(previous).IsFailure) return false;
        return true;
    }

    private Result<int> ValidateStep(int step)
    {
        if (step == 0)
            return Draft.Choice is null ? Result.Failure<int>("Выберите культуру и сорт")
                : GrowingConditions.Create(Draft.Location, Draft.Placement).Bind(_ => Result.Success(0));
        if (step == 1)
            return PlanningInputParser.Window(Draft.DirectSow ? Draft.DirectStartText : Draft.TransplantStartText,
                    Draft.DirectSow ? Draft.DirectEndText : Draft.TransplantEndText)
                .Bind(_ => PlanningInputParser.OptionalDate(Draft.ChosenSowingText, "Выбранный посев"))
                .Bind(_ => Draft.DirectSow ? Result.Success(Option.None<DateOnly>())
                    : PlanningInputParser.OptionalDate(Draft.ChosenTransplantText, "Выбранная высадка"))
                .Bind(_ => Result.Success(1));
        if (step == 2)
        {
            if (Draft.Harvest.Mode is HarvestPlanningMode.BedCapacity or HarvestPlanningMode.TargetPerM2 && !Draft.Harvest.UseLayout)
                return Result.Failure<int>("Укажите размеры грядки и схему: включите расчёт по прямоугольнику или возьмите размеры выбранной грядки");
            return Draft.Harvest.Build().Bind(input =>
                input.Mode is HarvestPlanningMode.TargetPerPlant or HarvestPlanningMode.TargetPerM2 && !input.TargetKg.Match(x => x > 0, () => false)
                    ? Result.Failure<int>("Цель урожая, кг: укажите положительное число")
                    : (Draft.Harvest.IsManualPlants ? PlanningInputParser.Count(Draft.TargetPlantsText, "Количество растений") : PlantCount.Create(1))
                    .Bind(_ => PlanningInputParser.OptionalCount(Draft.ManualSeedCountText, "Количество семян"))
                    .Bind(_ => Result.Success(2)));
        }
        return Draft.BuildInput().Bind(input =>
        {
            var context = _referenceContext;
            if (context is null || context.SortId != input.SortId || context.Culture.Id != input.CultureId)
                return Result.Success(step);
            return CultureId.Create(input.CultureId).Bind(id => context.CultureProfile.Match(Result.Success,
                    () => CultureGrowingProfile.Create(id, new GrowingProfileParameters(), new PlanningExtras())))
                .Bind(culture => GrowingProfileResolver.FindMissingInputs(culture, context.SortCharacteristics,
                    input.Overrides, Draft.DirectSow))
                .Bind(missing => missing.Length == 0 ? Result.Success(step)
                    : Result.Failure<int>(string.Join("; ", missing) + ". Заполните параметры здесь или в справочнике."));
        });
    }

    private void RefreshSteps()
    {
        while (_editorStep > 0 && !CanOpenStep(_editorStep)) _editorStep--;
        OnPropertyChanged(nameof(EditorStep));
        OnPropertyChanged(nameof(DatesStepEnabled));
        OnPropertyChanged(nameof(QuantityStepEnabled));
        OnPropertyChanged(nameof(ParametersStepEnabled));
        OnPropertyChanged(nameof(ResultStepEnabled));
        OnPropertyChanged(nameof(IsResultStep));
        OnPropertyChanged(nameof(HasNextStep));
        OnPropertyChanged(nameof(StepTitle));
        OnPropertyChanged(nameof(StepGuidance));
        OnPropertyChanged(nameof(CalculationGuidance));
        OnPropertyChanged(nameof(SaveGuidance));
        OnPropertyChanged(nameof(SelectedSowingText));
        OnPropertyChanged(nameof(EditorAvailable));
        CommandManager.InvalidateRequerySuggested();
    }
}
