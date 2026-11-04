using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models.Planning;

namespace Bonfire.ViewModels;

public sealed partial class PlantingPlanningViewModel
{
    private int _editorStep;
    private string _calculationValidation = "";
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
    public string NextStepLabel => EditorStep == 3 ? "Проверить и рассчитать" : "Далее";
    public bool EditorAvailable => !IsBusy;
    public bool NeedsSeedQuantity => Draft.ManualSeedCountText.Length == 0
        && (Draft.Profile.GerminationPercentRule.Validation.IsFailure || Draft.Profile.LossPercentRule.Validation.IsFailure)
        || (Preview?.Document.Match(document => document.Quantity is Option<SeedQuantityCalculation>.None, () => false) ?? false);
    public ICommand EditManualSeedCountCommand => field ??= new LambdaCommand(() => EditorStep = 2,
        () => !IsBusy && QuantityStepEnabled);
    public ICommand EditSeedPercentagesCommand => field ??= new LambdaCommand(() => EditorStep = 3,
        () => !IsBusy && ParametersStepEnabled);
    public string SaveGuidance => CanSave() ? "Сохранить рассчитанную партию в сезонный план"
        : !Draft.IsDirty ? "Нет изменений для сохранения."
        : NeedsSeedQuantity ? CalculationGuidance
        : Preview is null ? "Сначала выполните расчёт."
        : Preview.MissingInputs.Length > 0 ? CalculationGuidance : "Проверьте параметры и пересчитайте изменённый ввод.";
    public string StepTitle => $"Шаг {EditorStep + 1} из 5";
    public string SelectedSowingText => Draft.ChosenSowingText.Length == 0
        ? EditorStep switch
        {
            1 => "Дату посева можно задать в разделе «Ручной выбор дат» или выбрать из рекомендаций на шаге «Результат» после расчёта.",
            4 when Candidates.Count > 0 => "Чтобы выбрать день посева, нажмите «Выбрать посев» у нужной даты в списке рекомендаций на этом шаге.",
            4 => "Для ручного выбора дня посева вернитесь на шаг «Сроки», в раздел «Ручной выбор дат».",
            _ => ""
        }
        : "Выбран посев: " + Draft.ChosenSowingText
          + (Draft.Values.SowingOrigin == PlannedDateOrigin.LunarRecommendation ? " (из лунных рекомендаций). " : " (вручную). ")
          + (CanSave() ? IsResultStep ? "Для записи в план нажмите «Сохранить партию»." : ""
              : "Сохранение недоступно: " + SaveGuidance);
    public string StepGuidance => IsBusy ? "Выполняется операция…" : OpenedPlan is null
        ? "Сначала создайте или выберите сезонный план слева."
        : EditorStep == 3 && _calculationValidation.Length > 0 ? "Для продолжения: " + _calculationValidation
        : ValidateStep(EditorStep).Match(_ => EditorStep switch
        {
            0 => "Выберите способ посева и условия выращивания, затем нажмите «Далее».",
            1 => "Обе границы включены в период. Региональная справка помогает выбрать даты; рабочий интервал задаёте вы.",
            2 => "Выберите способ определения количества. Грядка необязательна при ручном количестве растений.",
            3 => "Пустые параметры наследуются из сорта и культуры. Нажмите «Проверить и рассчитать», чтобы продолжить.",
            _ => "Проверьте прогноз. Для выбора ручной даты вернитесь к срокам; для исправления параметров — на предыдущий шаг."
        }, error => "Для продолжения: " + error);

    public string CalculationGuidance => IsBusy ? "Выполняется расчёт…" : OpenedPlan is null
        ? "Выберите сезонный план."
        : NeedsSeedQuantity ? "Не рассчитано количество семян. Введите число семян на шаге «Количество» либо задайте всхожесть и потери на шаге «Параметры» (0% потерь — без потерь). Затем пересчитайте."
        : Draft.BuildInput().Match(_ => Preview is { MissingInputs.Length: > 0 } preview
            ? "Не хватает данных: " + string.Join("; ", preview.MissingInputs) + ". Заполните их на шаге «Параметры»."
            : Preview is null ? "Выполните расчёт перед сохранением." : "Расчёт готов. Сохраните партию, чтобы включить её в план.",
            error => "Расчёт недоступен: " + error);

    public ICommand PreviousStepCommand => field ??= new LambdaCommand(() => EditorStep--, () => !IsBusy && EditorStep > 0);
    public ICommand NextStepCommand => field ??= new LambdaCommandAsync(NextStepAsync,
        () => !IsBusy && EditorStep < 4 && (EditorStep == 3 ? CanOpenStep(3) && ValidateStep(3).IsSuccess : CanOpenStep(EditorStep + 1)));

    private async Task NextStepAsync()
    {
        if (EditorStep == 3)
        {
            if (!CanOpenStep(3) || ValidateStep(3).IsFailure) return;
            await CalculateAsync();
            if (ResultStepEnabled) EditorStep = 4;
            return;
        }
        if (CanOpenStep(EditorStep + 1)) EditorStep++;
    }

    private bool CanOpenStep(int step)
    {
        if (step == 0) return true;
        if (OpenedPlan is null) return false;
        for (var previous = 0; previous < step; previous++)
            if (ValidateStep(previous).IsFailure) return false;
        return step != 4 || Preview is { MissingInputs.Length: 0 } preview
            && preview.Document.Match(document => document.Quantity is Option<SeedQuantityCalculation>.Some, () => false)
            && Draft.BuildInput().Match(input => input == preview.Input, _ => false);
    }

    private Result<int> ValidateStep(int step)
    {
        if (step == 0)
            return Draft.ValidateFields(nameof(Draft.Choice), nameof(Draft.Location), nameof(Draft.Placement)).Bind(_ => Result.Success(0));
        if (step == 1)
            return Draft.ValidateFields(Draft.DirectSow ? nameof(Draft.DirectStartText) : nameof(Draft.TransplantStartText),
                    Draft.DirectSow ? nameof(Draft.DirectEndText) : nameof(Draft.TransplantEndText))
                .Bind(_ => PlanningInputParser.OptionalDate(Draft.ChosenSowingText, "Выбранный посев"))
                .Bind(_ => Draft.DirectSow ? Result.Success(Option.None<DateOnly>())
                    : PlanningInputParser.OptionalDate(Draft.ChosenTransplantText, "Выбранная высадка"))
                .Bind(_ => Result.Success(1));
        if (step == 2)
            return Draft.Harvest.Build().Bind(_ => Draft.ValidateFields(nameof(Draft.TargetPlantsText)))
                .Bind(_ => PlanningInputParser.OptionalCount(Draft.ManualSeedCountText, "Количество семян"))
                .Bind(_ => Result.Success(2));
        return Draft.BuildInput().Bind(_ => Result.Success(step));
    }

    private void RefreshSteps()
    {
        while (!IsBusy && _editorStep > 0 && !CanOpenStep(_editorStep)) _editorStep--;
        OnPropertyChanged(nameof(EditorStep));
        OnPropertyChanged(nameof(DatesStepEnabled));
        OnPropertyChanged(nameof(QuantityStepEnabled));
        OnPropertyChanged(nameof(ParametersStepEnabled));
        OnPropertyChanged(nameof(ResultStepEnabled));
        OnPropertyChanged(nameof(IsResultStep));
        OnPropertyChanged(nameof(HasNextStep));
        OnPropertyChanged(nameof(NextStepLabel));
        OnPropertyChanged(nameof(StepTitle));
        OnPropertyChanged(nameof(StepGuidance));
        OnPropertyChanged(nameof(CalculationGuidance));
        OnPropertyChanged(nameof(NeedsSeedQuantity));
        OnPropertyChanged(nameof(SaveGuidance));
        OnPropertyChanged(nameof(SelectedSowingText));
        OnPropertyChanged(nameof(EditorAvailable));
        CommandManager.InvalidateRequerySuggested();
    }
}
