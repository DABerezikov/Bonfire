using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Application.Planning;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.Regions;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models.Mappers;
using Bonfire.Models.Planning;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;

namespace Bonfire.ViewModels;

public sealed class PlantingPlanningViewModel : ViewModel
{
    private readonly IPlanningService _service;
    private readonly IUserDialog _dialog;
    private bool _applying;
    private long _openRevision;
    private long _referenceRevision;
    private PlannedBatchEditorDraft _draft = new(0);
    public PlanningLibrariesViewModel Libraries { get; }
    public ObservableCollection<PlantingPlanSummaryFromViewModel> Plans { get; } = [];
    public ObservableCollection<PlannedBatchListItem> Batches { get; } = [];
    public ObservableCollection<RegionFromViewModel> Regions { get; } = [];
    public ObservableCollection<PlanningChoiceFromViewModel> Choices { get; } = [];
    public ObservableCollection<SowingCandidateItem> Candidates { get; } = [];
    public ObservableCollection<RegionalWindowItem> References { get; } = [];
    public string NewPlanName { get; set => Set(ref field, value); } = "Новый сезон";
    public string NewPlanYear { get; set => Set(ref field, value); }
    public RegionFromViewModel? NewPlanRegion { get; set => Set(ref field, value); }
    public bool AcceptHistoricalSnapshot { get; set => Set(ref field, value); }
    public string Status { get; private set => Set(ref field, value); } = "Создайте или выберите сезонный план";
    public string PreviewText { get; private set => Set(ref field, value); } = "";
    public string ReferenceText { get; private set => Set(ref field, value); } = "Региональные сроки справочные; рабочий интервал задаётся вручную.";
    public bool IsBusy { get; private set => Set(ref field, value); }
    public PlantingPlanDetailsFromViewModel? OpenedPlan { get; private set => Set(ref field, value); }
    public PlanningPreviewFromViewModel? Preview { get; private set => Set(ref field, value); }
    public PlannedBatchEditorDraft Draft => _draft;
    public PlantingPlanSummaryFromViewModel? SelectedPlan
    {
        get;
        set { if (Set(ref field, value) && !_applying && value is not null) _ = OpenPlanAsync(value); }
    }
    public PlannedBatchListItem? SelectedBatch
    {
        get;
        set
        {
            if (!Set(ref field, value) || _applying || value is null || OpenedPlan is null) return;
            Edit(value);
        }
    }
    public bool IsActive
    {
        get;
        set { if (Set(ref field, value) && value && Plans.Count == 0) _ = LoadAsync(); }
    }

    public PlantingPlanningViewModel(IPlanningService service, IUserDialog dialog, PlanningLibrariesViewModel libraries, TimeProvider clock)
    {
        _service = service; _dialog = dialog; Libraries = libraries;
        NewPlanYear = clock.GetLocalNow().Year.ToString();
        _draft.PropertyChanged += DraftChanged;
    }

    public ICommand LoadCommand => field ??= new LambdaCommandAsync(LoadAsync, () => !IsBusy);
    public ICommand CreatePlanCommand => field ??= new LambdaCommandAsync(CreatePlanAsync, () => !IsBusy && NewPlanRegion is not null);
    public ICommand NewBatchCommand => field ??= new LambdaCommand(() => NewBatch(), () => !IsBusy && OpenedPlan is not null);
    public ICommand CalculateCommand => field ??= new LambdaCommandAsync(CalculateAsync, () => !IsBusy && OpenedPlan is not null && Draft.BuildInput().IsSuccess);
    public ICommand SaveCommand => field ??= new LambdaCommandAsync(SaveAsync, CanSave);
    public ICommand CancelCommand => field ??= new LambdaCommand(Cancel);
    public ICommand DeleteBatchCommand => field ??= new LambdaCommandAsync(DeleteBatchAsync, () => !IsBusy && OpenedPlan is not null && SelectedBatch is not null);
    public ICommand DeletePlanCommand => field ??= new LambdaCommandAsync(DeletePlanAsync, () => !IsBusy && OpenedPlan is not null);
    public ICommand ChooseSowingCommand => field ??= new LambdaCommandAsync(async parameter =>
    {
        if (parameter is not SowingCandidateItem candidate || IsBusy) return;
        Draft.ChooseLunarDate(candidate.Candidate.Date);
        await CalculateAsync();
    });
    public ICommand UseReferenceCommand => field ??= new LambdaCommand(parameter =>
    {
        if (parameter is not RegionalWindowItem item) return;
        item.Reference.Window.Project(OpenedPlan?.Definition.SeasonYear ?? DateTime.Now.Year).Match(window =>
        {
            if (Draft.DirectSow) { Draft.DirectStartText = PlanningInputParser.Format(window.Start); Draft.DirectEndText = PlanningInputParser.Format(window.End); }
            else { Draft.TransplantStartText = PlanningInputParser.Format(window.Start); Draft.TransplantEndText = PlanningInputParser.Format(window.End); }
            return 0;
        }, Error);
    }, parameter => parameter is RegionalWindowItem item && item.Reference.Conditions.Location == Draft.Location && item.Reference.Conditions.Placement == Draft.Placement);

    public async Task LoadAsync()
    {
        var plans = await _service.ReadPlansAsync();
        plans.Match(items =>
        {
            var id = SelectedPlan?.Id;
            _applying = true;
            try { Plans.Clear(); foreach (var item in items) Plans.Add(item); SelectedPlan = Plans.FirstOrDefault(x => x.Id == id); }
            finally { _applying = false; }
            return 0;
        }, Error);
        var regions = await _service.ReadRegionsAsync();
        regions.Match(items => { var id = NewPlanRegion?.Id; Regions.Clear(); foreach (var item in items) Regions.Add(item);
            NewPlanRegion = Regions.FirstOrDefault(x => x.Id == id) ?? Regions.FirstOrDefault(x => x.Definition.Name.Value == "Томск") ?? Regions.FirstOrDefault(); return 0; }, Error);
        var choices = await _service.ReadChoicesAsync();
        choices.Match(items => { Choices.Clear(); foreach (var item in items) Choices.Add(item); EnsureChoice(Draft.Choice); return 0; }, Error);
        if (OpenedPlan is null && Plans.Count > 0) await OpenPlanAsync(Plans[0]);
    }

    public async Task OpenPlanAsync(PlantingPlanSummaryFromViewModel summary)
    {
        _applying = true; try { SelectedPlan = summary; } finally { _applying = false; }
        var revision = ++_openRevision;
        var result = await _service.ReadPlanAsync(summary.Id);
        if (revision != _openRevision || SelectedPlan?.Id != summary.Id) return;
        result.Match(found => found.Match(plan =>
        {
            OpenedPlan = plan; ApplyBatches(plan); NewBatch(); Status = "План открыт"; return 0;
        }, () => Error("План не найден")), Error);
    }

    private async Task CreatePlanAsync()
    {
        var region = NewPlanRegion;
        if (region is null || !int.TryParse(NewPlanYear, out var year)) { Status = "Выберите регион и укажите год"; return; }
        IsBusy = true;
        try
        {
            var result = await _service.CreatePlanAsync(NewPlanName, year, region.Id.Value);
            result.Match(plan =>
            {
                var summary = Summary(plan); Plans.Add(summary); _applying = true;
                try { SelectedPlan = summary; } finally { _applying = false; }
                OpenedPlan = plan; ApplyBatches(plan); NewBatch(); Status = "План создан"; return 0;
            }, Error);
        }
        finally { IsBusy = false; CommandManager.InvalidateRequerySuggested(); }
    }

    public async Task CalculateAsync()
    {
        if (IsBusy || OpenedPlan is null) return;
        var draft = Draft;
        var revision = draft.Revision;
        var input = draft.BuildInput();
        if (input.IsFailure) { Status = PlanningInputParser.Error(input); return; }
        IsBusy = true;
        try
        {
            var result = await _service.CalculateAsync(input.ValueOrThrow());
            if (!ReferenceEquals(Draft, draft) || draft.Revision != revision) return;
            result.Match(preview => { ApplyPreview(preview); Status = preview.MissingInputs.Length == 0 ? "Расчёт готов. Выбор даты и сохранение — отдельные действия." : "Заполните недостающие параметры"; return 0; }, Error);
        }
        finally { IsBusy = false; CommandManager.InvalidateRequerySuggested(); }
    }

    public async Task SaveAsync()
    {
        var plan = OpenedPlan; var preview = Preview; var draft = Draft;
        if (IsBusy || plan is null || preview is null) return;
        var input = draft.BuildInput();
        if (input.IsFailure) { Status = PlanningInputParser.Error(input); return; }
        var submitted = draft.Values;
        IsBusy = true;
        try
        {
            var result = await _service.SaveBatchAsync(new(plan.Id, plan.Version, draft.BatchId, input.ValueOrThrow(), preview, AcceptHistoricalSnapshot));
            result.Match(saved =>
            {
                UpdateSummary(saved);
                if (OpenedPlan?.Id == plan.Id)
                {
                    OpenedPlan = saved; ApplyBatches(saved);
                    if (ReferenceEquals(Draft, draft))
                    {
                        var id = draft.BatchId.Match(x => x, () => saved.Batches.Last().Id);
                        draft.AcceptSaved(submitted, id);
                        _applying = true;
                        try { SelectedBatch = Batches.FirstOrDefault(x => x.Id == id); } finally { _applying = false; }
                    }
                }
                Status = "Партия сохранена"; return 0;
            }, Error);
        }
        finally { IsBusy = false; CommandManager.InvalidateRequerySuggested(); }
    }

    private bool CanSave() => !IsBusy && Draft.IsDirty && OpenedPlan is not null && Preview is { } preview && preview.MissingInputs.Length == 0
        && Draft.BuildInput().Match(input => input == preview.Input, _ => false);
    public void NewBatch()
    {
        _applying = true; try { SelectedBatch = null; } finally { _applying = false; }
        SetDraft(new(OpenedPlan?.Definition.RegionId.Value ?? 0));
    }
    public void Cancel() { if (SelectedBatch is { } selected) Edit(selected); else NewBatch(); }

    private void Edit(PlannedBatchListItem item)
    {
        if (OpenedPlan is null) return;
        var draft = PlanningMapper.Draft(OpenedPlan.Definition.RegionId.Value, item.Snapshot);
        EnsureChoice(draft.Choice); SetDraft(draft);
        var input = draft.BuildInput();
        if (input.IsSuccess)
        {
            var document = item.Snapshot.Document;
            ApplyPreview(new(input.ValueOrThrow(), "", document.References, Option.Some(document), [], document.CalculatedAt));
            Status = "Сохранённый расчёт. После изменения ввода выполните новый расчёт.";
        }
    }

    private async Task DeleteBatchAsync()
    {
        var plan = OpenedPlan; var selected = SelectedBatch; var draft = Draft;
        if (plan is null || selected is null || !_dialog.YesNoQuestion("Удалить эту плановую партию?", "Планирование")) return;
        IsBusy = true;
        try
        {
            (await _service.DeleteBatchAsync(plan.Id, plan.Version, selected.Id)).Match(saved =>
            {
                UpdateSummary(saved);
                if (OpenedPlan?.Id == plan.Id) { OpenedPlan = saved; ApplyBatches(saved); if (ReferenceEquals(Draft, draft)) NewBatch(); }
                Status = "Плановая партия удалена"; return 0;
            }, Error);
        }
        finally { IsBusy = false; }
    }
    private async Task DeletePlanAsync()
    {
        var plan = OpenedPlan;
        if (plan is null || !_dialog.YesNoQuestion("Удалить сезонный план и его плановые партии?", "Планирование")) return;
        IsBusy = true;
        try
        {
            (await _service.DeletePlanAsync(plan.Id, plan.Version)).Match(_ =>
            {
                var summary = Plans.FirstOrDefault(x => x.Id == plan.Id); if (summary is not null) Plans.Remove(summary);
                if (OpenedPlan?.Id == plan.Id) { OpenedPlan = null; Batches.Clear(); NewBatch(); }
                Status = "План удалён"; return 0;
            }, Error);
        }
        finally { IsBusy = false; }
    }

    private void SetDraft(PlannedBatchEditorDraft draft)
    {
        _draft.PropertyChanged -= DraftChanged; _draft = draft; _draft.PropertyChanged += DraftChanged;
        Preview = null; PreviewText = ""; Candidates.Clear(); References.Clear(); AcceptHistoricalSnapshot = false;
        OnPropertyChanged(nameof(Draft)); CommandManager.InvalidateRequerySuggested();
        if (draft.Choice is not null) _ = RefreshReferenceAsync();
    }
    private void DraftChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(PlannedBatchEditorDraft.Values))
        {
            Preview = null; PreviewText = "Ввод изменён — выполните расчёт"; Candidates.Clear();
            CommandManager.InvalidateRequerySuggested();
        }
        if (args.PropertyName is nameof(PlannedBatchEditorDraft.Choice) or nameof(PlannedBatchEditorDraft.DirectSow)) _ = RefreshReferenceAsync();
    }
    private async Task RefreshReferenceAsync()
    {
        var draft = Draft; var choice = draft.Choice; var revision = ++_referenceRevision;
        if (choice is null || OpenedPlan is null) return;
        var result = await _service.ReadContextAsync(draft.RegionId, choice.CultureId, choice.SortId);
        if (revision != _referenceRevision || !ReferenceEquals(Draft, draft)) return;
        result.Match(context =>
        {
            ApplyReferences(context.RegionalWindows, draft.DirectSow);
            var reference = context.Region.Definition.Reference;
            var year = OpenedPlan?.Definition.SeasonYear ?? DateTime.Now.Year;
            var purpose = draft.DirectSow ? RegionalWindowPurpose.DirectSowing : RegionalWindowPurpose.Transplanting;
            var extras = GrowingProfileResolver.ResolveExtras(context.CultureProfile.Match(value => value.Extras, () => new()),
                context.SortSettings.Match(value => value.Extras, () => new()), new());
            ReferenceText = "Сроки справочные и не изменяют рабочий интервал.\n"
                + "Открытый грунт: " + (context.RegionalWindows.Any(x => x.Purpose == purpose && x.Conditions.Location == GrowingLocation.OpenGround) ? "см. интервалы ниже" : "интервал не заполнен") + ". "
                + "Теплица: " + (context.RegionalWindows.Any(x => x.Purpose == purpose && x.Conditions.Location == GrowingLocation.Greenhouse) ? "см. интервалы ниже" : "интервал не заполнен") + ".\n"
                + "Последние весенние заморозки: " + Climate(reference.LastSpringFrost, year) + "; первые осенние: " + Climate(reference.FirstAutumnFrost, year) + ".\n"
                + extras.ReferenceSoilTemperatureC.Match(value => $"Температура почвы (справочно): {value.Value:0.##} °C ({PlanningPresentation.Source(value.Source)}). ", () => "Справочная температура почвы не задана. ")
                + reference.Source.Match(value => "Источник региона: " + value, () => "Источник региона не указан.");
            return 0;
        }, error => { ReferenceText = error; return 0; });
    }
    private void ApplyPreview(PlanningPreviewFromViewModel preview)
    {
        Preview = preview; PreviewText = PlanningPresentation.Describe(preview); Candidates.Clear();
        preview.Document.Match(document => { foreach (var candidate in PlanningPresentation.Candidates(document.Preview.Lunar)) Candidates.Add(new(candidate)); return 0; }, () => 0);
        ApplyReferences(preview.References.RegionalWindows, preview.Input.Method is GrowingMethod.DirectSow);
    }
    private void ApplyReferences(System.Collections.Immutable.ImmutableArray<RegionalPlantingWindow> windows, bool direct)
    {
        References.Clear();
        foreach (var window in windows.Where(x => x.Purpose == (direct ? RegionalWindowPurpose.DirectSowing : RegionalWindowPurpose.Transplanting)))
            References.Add(new(window, OpenedPlan?.Definition.SeasonYear ?? DateTime.Now.Year));
    }
    private void EnsureChoice(PlanningChoiceFromViewModel? choice)
    {
        if (choice is not null && !Choices.Contains(choice)) Choices.Add(choice);
    }
    private void ApplyBatches(PlantingPlanDetailsFromViewModel plan)
    {
        var selectedId = SelectedBatch?.Id; _applying = true;
        try { Batches.Clear(); foreach (var batch in plan.Batches) Batches.Add(new(batch)); SelectedBatch = Batches.FirstOrDefault(x => x.Id == selectedId); }
        finally { _applying = false; }
    }
    private void UpdateSummary(PlantingPlanDetailsFromViewModel plan)
    {
        var current = Plans.FirstOrDefault(x => x.Id == plan.Id); var updated = Summary(plan);
        _applying = true;
        try { if (current is null) Plans.Add(updated); else Plans[Plans.IndexOf(current)] = updated; if (SelectedPlan?.Id == plan.Id) SelectedPlan = updated; }
        finally { _applying = false; }
    }
    private static PlantingPlanSummaryFromViewModel Summary(PlantingPlanDetailsFromViewModel plan) => new(plan.Id, plan.Version, plan.Definition.Name.Value, plan.Definition.SeasonYear, plan.Definition.RegionName);
    private static string Climate(Option<AnnualDateWindow> window, int year) => window.Match(value => value.Project(year).Match(PlanningPresentation.Window, error => error), () => "нет сведений");
    private int Error(string error) { Status = error; return 0; }
}
