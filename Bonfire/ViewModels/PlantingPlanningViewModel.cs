using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Application.Planning;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Planning.Regions;
using Bonfire.Infrastructure;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models.Mappers;
using Bonfire.Models.Planning;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;

namespace Bonfire.ViewModels;

public sealed partial class PlantingPlanningViewModel : ViewModel
{
    private readonly IUserDialog _dialog;
    private readonly IPlanningService _service;
    private bool _applying;
    private long _openRevision;
    private PlanningContextFromViewModel? _referenceContext;
    private long _referenceRevision;

    public PlantingPlanningViewModel(IPlanningService service, IUserDialog dialog, PlanningLibrariesViewModel libraries,
        TimeProvider clock)
    {
        _service = service;
        _dialog = dialog;
        Libraries = libraries;
        NewPlanYear = clock.GetLocalNow().Year.ToString();
        Draft.PropertyChanged += DraftChanged;
    }

    public PlanningLibrariesViewModel Libraries { get; }
    public ObservableCollection<PlantingPlanSummaryFromViewModel> Plans { get; } = [];
    public ObservableCollection<PlannedBatchListItem> Batches { get; } = [];
    public ObservableCollection<RegionFromViewModel> Regions { get; } = [];
    public ObservableCollection<PlanningChoiceFromViewModel> Choices { get; } = [];
    public ObservableCollection<PlanningBedFromViewModel> Beds { get; } = [];
    public PlanningBedFromViewModel? SelectedBed { get; set; }

    public ICommand UseBedCommand => field ??= new LambdaCommand(() =>
    {
        if (SelectedBed is not { } bed) return;
        CanvasGeometryMapper.PlanningSize(bed.WidthPixels, bed.HeightPixels, CanvasConstants.PixelsPerMeter).Match(
            size =>
            {
                Draft.Harvest.ApplyBed(bed.Id, bed.Name, size.Length, size.Width);
                return 0;
            }, Error);
    }, () => SelectedBed is not null);

    public ICommand HalfMarginsCommand => field ??= new LambdaCommand(() => Draft.Harvest.HalfStepMargins());

    public ICommand UseSortSpacingCommand => field ??= new LambdaCommand(() =>
    {
        if (_referenceContext is not { } context || Draft.Choice?.SortId != context.SortId ||
            Draft.Choice?.CultureId != context.Culture.Id) return;
        if (context.SortCharacteristics.LandingPattern is not Option<int>.Some plant || plant.Value <= 0 ||
            context.SortCharacteristics.LandingRowSpacing is not Option<int>.Some row || row.Value <= 0)
        {
            Status = "В сорте не заполнены оба положительных шага посадки в см";
            return;
        }

        Draft.Harvest.PlantStep = (plant.Value / 100m).ToString();
        Draft.Harvest.RowStep = (row.Value / 100m).ToString();
        Status = "Шаги взяты из схемы сорта (см → м). Проверьте отступы перед расчётом";
    });

    public string SeasonHarvestText => PlanningPresentation.SeasonHarvest(Batches.Select(x => x.Snapshot));
    public ObservableCollection<SowingCandidateItem> Candidates { get; } = [];
    public ObservableCollection<RegionalWindowItem> References { get; } = [];

    public string NewPlanName
    {
        get;
        set { if (Set(ref field, value)) RefreshPlanRules(); }
    } = "Новый сезон";

    public string NewPlanYear
    {
        get;
        set { if (Set(ref field, value)) RefreshPlanRules(); }
    }

    public RegionFromViewModel? NewPlanRegion
    {
        get;
        set { if (Set(ref field, value)) RefreshPlanRules(); }
    }

    public bool AcceptHistoricalSnapshot
    {
        get;
        set => Set(ref field, value);
    }

    public string Status
    {
        get;
        private set => Set(ref field, value);
    } = "Создайте или выберите сезонный план";

    public string PreviewText
    {
        get;
        private set => Set(ref field, value);
    } = "";

    public string ReferenceText
    {
        get;
        private set => Set(ref field, value);
    } = "Региональные сроки справочные; рабочий интервал задаётся вручную.";

    public bool IsBusy
    {
        get;
        private set { if (Set(ref field, value)) RefreshSteps(); }
    }

    public PlantingPlanDetailsFromViewModel? OpenedPlan
    {
        get;
        private set { if (Set(ref field, value)) RefreshSteps(); }
    }

    public PlanningPreviewFromViewModel? Preview
    {
        get;
        private set => Set(ref field, value);
    }

    public PlannedBatchEditorDraft Draft { get; private set; } = new(0);

    public PlantingPlanSummaryFromViewModel? SelectedPlan
    {
        get;
        set
        {
            if (Set(ref field, value) && !_applying && value is not null) _ = OpenPlanAsync(value);
        }
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
        set
        {
            if (Set(ref field, value) && value && Plans.Count == 0) _ = LoadAsync();
        }
    }

    public ICommand LoadCommand => field ??= new LambdaCommandAsync(LoadAsync, () => !IsBusy);

    public ICommand CreatePlanCommand =>
        field ??= new LambdaCommandAsync(CreatePlanAsync, () => !IsBusy && PlanningFieldRule.ValidateAll(NewPlanFieldRules.Values).IsSuccess);

    public ICommand NewBatchCommand =>
        field ??= new LambdaCommand(() => NewBatch(), () => !IsBusy && OpenedPlan is not null);

    public ICommand CalculateCommand => field ??= new LambdaCommandAsync(CalculateAsync,
        () => !IsBusy && OpenedPlan is not null && Draft.BuildInput().IsSuccess);

    public ICommand SaveCommand => field ??= new LambdaCommandAsync(SaveAsync, CanSave);
    public ICommand CancelCommand => field ??= new LambdaCommand(Cancel);

    public ICommand DeleteBatchCommand => field ??= new LambdaCommandAsync(DeleteBatchAsync,
        () => !IsBusy && OpenedPlan is not null && SelectedBatch is not null);

    public ICommand DeletePlanCommand =>
        field ??= new LambdaCommandAsync(DeletePlanAsync, () => !IsBusy && OpenedPlan is not null);

    public ICommand ChooseSowingCommand => field ??= new LambdaCommandAsync(async parameter =>
    {
        if (parameter is not SowingCandidateItem candidate || IsBusy) return;
        var returnToResult = IsResultStep;
        Draft.ChooseLunarDate(candidate.Candidate.Date);
        await CalculateAsync();
        if (returnToResult && ResultStepEnabled) EditorStep = 4;
    }, parameter => !IsBusy && parameter is SowingCandidateItem { IsSelected: false });

    public ICommand UseReferenceCommand => field ??= new LambdaCommand(parameter =>
        {
            if (parameter is not RegionalWindowItem item) return;
            item.Reference.Window.Project(OpenedPlan?.Definition.SeasonYear ?? DateTime.Now.Year).Match(window =>
            {
                if (Draft.DirectSow)
                {
                    Draft.DirectStartText = PlanningInputParser.Format(window.Start);
                    Draft.DirectEndText = PlanningInputParser.Format(window.End);
                }
                else
                {
                    Draft.TransplantStartText = PlanningInputParser.Format(window.Start);
                    Draft.TransplantEndText = PlanningInputParser.Format(window.End);
                }

                return 0;
            }, Error);
        },
        parameter => parameter is RegionalWindowItem item && item.Reference.Conditions.Location == Draft.Location &&
                     item.Reference.Conditions.Placement == Draft.Placement);

    public async Task LoadAsync()
    {
        var plans = await _service.ReadPlansAsync();
        plans.Match(items =>
        {
            var id = SelectedPlan?.Id;
            _applying = true;
            try
            {
                Plans.Clear();
                foreach (var item in items) Plans.Add(item);
                SelectedPlan = Plans.FirstOrDefault(x => x.Id == id);
            }
            finally
            {
                _applying = false;
            }

            return 0;
        }, Error);
        var regions = await _service.ReadRegionsAsync();
        regions.Match(items =>
        {
            var id = NewPlanRegion?.Id;
            Regions.Clear();
            foreach (var item in items) Regions.Add(item);
            NewPlanRegion = Regions.FirstOrDefault(x => x.Id == id) ??
                            Regions.FirstOrDefault(x => x.Definition.Name.Value == "Томск") ?? Regions.FirstOrDefault();
            return 0;
        }, Error);
        var choices = await _service.ReadChoicesAsync();
        var choice = Draft.Choice;
        choices.Match(items =>
        {
            Choices.Clear();
            foreach (var item in items) Choices.Add(item);
            EnsureChoice(choice);
            Draft.Choice = choice;
            return 0;
        }, Error);
        (await _service.ReadBedsAsync()).Match(items =>
        {
            Beds.Clear();
            foreach (var item in items) Beds.Add(item);
            return 0;
        }, Error);
        if (plans.IsFailure) return;
        if (OpenedPlan is { } opened)
        {
            var refreshed = Plans.FirstOrDefault(x => x.Id == opened.Id);
            if (refreshed is null) Status = "План больше не найден; черновик сохранён";
            else if (Draft.IsDirty)
                Status = "Справочники обновлены; черновик сохранён. Для перечитывания плана сначала отмените изменения";
            else await OpenPlanAsync(refreshed);
        }
        else if (Plans.Count > 0)
        {
            await OpenPlanAsync(Plans[0]);
        }
    }

    public async Task OpenPlanAsync(PlantingPlanSummaryFromViewModel summary)
    {
        _applying = true;
        try
        {
            SelectedPlan = summary;
        }
        finally
        {
            _applying = false;
        }

        var revision = ++_openRevision;
        var openingDraft = Draft;
        var draftRevision = openingDraft.Revision;
        var result = await _service.ReadPlanAsync(summary.Id);
        if (revision != _openRevision || SelectedPlan?.Id != summary.Id) return;
        if (!ReferenceEquals(Draft, openingDraft) || openingDraft.Revision != draftRevision)
        {
            _applying = true;
            try
            {
                SelectedPlan = Plans.FirstOrDefault(x => x.Id == OpenedPlan?.Id);
            }
            finally
            {
                _applying = false;
            }

            Status = "План не заменён: во время чтения изменился черновик";
            return;
        }

        result.Match(found => found.Match(plan =>
        {
            OpenedPlan = plan;
            ApplyBatches(plan);
            NewBatch();
            Status = "План открыт";
            return 0;
        }, () => Error("План не найден")), Error);
    }

    private async Task CreatePlanAsync()
    {
        var region = NewPlanRegion;
        if (region is null || !int.TryParse(NewPlanYear, out var year))
        {
            Status = "Выберите регион и укажите год";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _service.CreatePlanAsync(NewPlanName, year, region.Id.Value);
            result.Match(plan =>
            {
                var summary = Summary(plan);
                Plans.Add(summary);
                _applying = true;
                try
                {
                    SelectedPlan = summary;
                }
                finally
                {
                    _applying = false;
                }

                OpenedPlan = plan;
                ApplyBatches(plan);
                NewBatch();
                Status = "План создан";
                return 0;
            }, Error);
        }
        finally
        {
            IsBusy = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public async Task CalculateAsync()
    {
        if (IsBusy || OpenedPlan is null) return;
        var draft = Draft;
        var revision = draft.Revision;
        var input = draft.BuildInput();
        if (input.IsFailure)
        {
            Status = PlanningInputParser.Error(input);
            return;
        }

        IsBusy = true;
        try
        {
            Preview = null;
            _calculationValidation = "";
            var result = await _service.CalculateAsync(input.ValueOrThrow());
            if (!ReferenceEquals(Draft, draft) || draft.Revision != revision) return;
            result.Match(preview =>
            {
                ApplyPreview(preview);
                _calculationValidation = string.Join("; ", preview.MissingInputs);
                RefreshSteps();
                Status = preview.MissingInputs.Length == 0
                    ? "Расчёт готов. Выбор даты и сохранение — отдельные действия."
                    : "Заполните недостающие параметры";
                return 0;
            }, error => { _calculationValidation = error; return Error(error); });
        }
        finally
        {
            IsBusy = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public async Task SaveAsync()
    {
        var plan = OpenedPlan;
        var preview = Preview;
        var draft = Draft;
        if (IsBusy || plan is null || preview is null) return;
        var input = draft.BuildInput();
        if (input.IsFailure)
        {
            Status = PlanningInputParser.Error(input);
            return;
        }

        var submitted = draft.Values;
        IsBusy = true;
        try
        {
            var result = await _service.SaveBatchAsync(new SavePlannedBatchFromViewModel(plan.Id, plan.Version,
                draft.BatchId, input.ValueOrThrow(), preview, AcceptHistoricalSnapshot));
            result.Match(saved =>
            {
                UpdateSummary(saved);
                if (OpenedPlan?.Id == plan.Id)
                {
                    OpenedPlan = saved;
                    ApplyBatches(saved);
                    if (ReferenceEquals(Draft, draft))
                    {
                        var id = draft.BatchId.Match(x => x, () => saved.Batches.Last().Id);
                        draft.AcceptSaved(submitted, id);
                        _applying = true;
                        try
                        {
                            SelectedBatch = Batches.FirstOrDefault(x => x.Id == id);
                        }
                        finally
                        {
                            _applying = false;
                        }
                    }
                }

                Status = "Партия сохранена";
                return 0;
            }, Error);
        }
        finally
        {
            IsBusy = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private bool CanSave() => !IsBusy && Draft.IsDirty && ResultStepEnabled;

    public void NewBatch()
    {
        _applying = true;
        try
        {
            SelectedBatch = null;
        }
        finally
        {
            _applying = false;
        }

        SetDraft(new PlannedBatchEditorDraft(OpenedPlan?.Definition.RegionId.Value ?? 0));
    }

    public void Cancel()
    {
        if (SelectedBatch is { } selected) Edit(selected);
        else NewBatch();
    }

    private void Edit(PlannedBatchListItem item)
    {
        if (OpenedPlan is null) return;
        var draft = PlanningMapper.Draft(OpenedPlan.Definition.RegionId.Value, item.Snapshot);
        EnsureChoice(draft.Choice);
        SetDraft(draft);
        var input = draft.BuildInput();
        if (input.IsSuccess)
        {
            var document = item.Snapshot.Document;
            ApplyPreview(new PlanningPreviewFromViewModel(input.ValueOrThrow(), "", document.References,
                Option.Some(document), [], document.CalculatedAt));
            Status = "Сохранённый расчёт. После изменения ввода выполните новый расчёт.";
        }
    }

    private async Task DeleteBatchAsync()
    {
        var plan = OpenedPlan;
        var selected = SelectedBatch;
        var draft = Draft;
        if (plan is null || selected is null ||
            !_dialog.YesNoQuestion("Удалить эту плановую партию?", "Планирование")) return;
        IsBusy = true;
        try
        {
            (await _service.DeleteBatchAsync(plan.Id, plan.Version, selected.Id)).Match(saved =>
            {
                UpdateSummary(saved);
                if (OpenedPlan?.Id == plan.Id)
                {
                    OpenedPlan = saved;
                    ApplyBatches(saved);
                    if (ReferenceEquals(Draft, draft)) NewBatch();
                }

                Status = "Плановая партия удалена";
                return 0;
            }, Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeletePlanAsync()
    {
        var plan = OpenedPlan;
        if (plan is null ||
            !_dialog.YesNoQuestion("Удалить сезонный план и его плановые партии?", "Планирование")) return;
        IsBusy = true;
        try
        {
            (await _service.DeletePlanAsync(plan.Id, plan.Version)).Match(_ =>
            {
                var summary = Plans.FirstOrDefault(x => x.Id == plan.Id);
                if (summary is not null) Plans.Remove(summary);
                if (OpenedPlan?.Id == plan.Id)
                {
                    OpenedPlan = null;
                    Batches.Clear();
                    NewBatch();
                }

                Status = "План удалён";
                return 0;
            }, Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetDraft(PlannedBatchEditorDraft draft)
    {
        Draft.PropertyChanged -= DraftChanged;
        Draft = draft;
        Draft.PropertyChanged += DraftChanged;
        Preview = null;
        PreviewText = "";
        Candidates.Clear();
        References.Clear();
        AcceptHistoricalSnapshot = false;
        _calculationValidation = "";
        EditorStep = 0;
        RefreshSteps();
        OnPropertyChanged(nameof(Draft));
        CommandManager.InvalidateRequerySuggested();
        if (draft.Choice is not null) _ = RefreshReferenceAsync();
    }

    private void DraftChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(PlannedBatchEditorDraft.Values))
        {
            if (_referenceContext is { } context &&
                (context.Culture.Id != Draft.Choice?.CultureId || context.SortId != Draft.Choice?.SortId))
            {
                _referenceContext = null;
                Draft.Profile.ConfigureInheritance(new(), new(), Option.None<GerminationPercentage>(), Option.None<LossPercentage>());
                Draft.Harvest.ConfigureInheritance(Option.None<decimal>(), Option.None<decimal>());
            }
            Preview = null;
            _calculationValidation = "";
            PreviewText = "Ввод изменён — выполните расчёт";
            Candidates.Clear();
            RefreshSteps();
            CommandManager.InvalidateRequerySuggested();
        }

        if (args.PropertyName is nameof(PlannedBatchEditorDraft.Choice) or nameof(PlannedBatchEditorDraft.DirectSow))
            _ = RefreshReferenceAsync();
    }

    private async Task RefreshReferenceAsync()
    {
        var draft = Draft;
        var choice = draft.Choice;
        var revision = ++_referenceRevision;
        _referenceContext = null;
        if (choice is null || OpenedPlan is null) return;
        var result = await _service.ReadContextAsync(draft.RegionId, choice.CultureId, choice.SortId);
        if (revision != _referenceRevision || !ReferenceEquals(Draft, draft)) return;
        result.Match(context =>
        {
            _referenceContext = context;
            ApplyReferences(context.RegionalWindows, draft.DirectSow);
            var reference = context.Region.Definition.Reference;
            var year = OpenedPlan?.Definition.SeasonYear ?? DateTime.Now.Year;
            var purpose = draft.DirectSow ? RegionalWindowPurpose.DirectSowing : RegionalWindowPurpose.Transplanting;
            var extras = GrowingProfileResolver.ResolveExtras(
                context.CultureProfile.Match(value => value.Extras, () => new PlanningExtras()),
                context.SortSettings.Match(value => value.Extras, () => new PlanningExtras()), new PlanningOverrides());
            draft.Profile.ConfigureInheritance(context.CultureProfile.Match(value => value.Parameters, () => new GrowingProfileParameters()),
                context.SortCharacteristics, extras.GerminationPercentage.Map(value => value.Value), extras.LossPercentage.Map(value => value.Value));
            draft.Harvest.ConfigureInheritance(extras.YieldPerPlantKg.Map(value => value.Value), extras.YieldPerM2Kg.Map(value => value.Value));
            RefreshSteps();
            ReferenceText = "Сроки справочные и не изменяют рабочий интервал.\n"
                            + "Открытый грунт: " +
                            (context.RegionalWindows.Any(x =>
                                x.Purpose == purpose && x.Conditions.Location == GrowingLocation.OpenGround)
                                ? "см. интервалы ниже"
                                : "интервал не заполнен") + ". "
                            + "Теплица: " +
                            (context.RegionalWindows.Any(x =>
                                x.Purpose == purpose && x.Conditions.Location == GrowingLocation.Greenhouse)
                                ? "см. интервалы ниже"
                                : "интервал не заполнен") + ".\n"
                            + "Последние весенние заморозки: " + Climate(reference.LastSpringFrost, year) +
                            "; первые осенние: " + Climate(reference.FirstAutumnFrost, year) + ".\n"
                            + extras.ReferenceSoilTemperatureC.Match(
                                value =>
                                    $"Температура почвы (справочно): {value.Value:0.##} °C ({PlanningPresentation.Source(value.Source)}). ",
                                () => "Справочная температура почвы не задана. ")
                            + reference.Source.Match(value => "Источник региона: " + value,
                                () => "Источник региона не указан.");
            return 0;
        }, error =>
        {
            ReferenceText = error;
            return 0;
        });
    }

    private void ApplyPreview(PlanningPreviewFromViewModel preview)
    {
        Preview = preview;
        PreviewText = PlanningPresentation.Describe(preview);
        Candidates.Clear();
        preview.Document.Match(document =>
        {
            foreach (var candidate in PlanningPresentation.Candidates(document.Preview.Lunar))
                Candidates.Add(new SowingCandidateItem(candidate)
                {
                    IsSelected = preview.Input.Choices.SowingDate.Match(date => date == candidate.Date, () => false)
                });
            return 0;
        }, () => 0);
        ApplyReferences(preview.References.RegionalWindows, preview.Input.Method is GrowingMethod.DirectSow);
    }

    private void ApplyReferences(ImmutableArray<RegionalPlantingWindow> windows, bool direct)
    {
        References.Clear();
        foreach (var window in windows.Where(x =>
                     x.Purpose == (direct ? RegionalWindowPurpose.DirectSowing : RegionalWindowPurpose.Transplanting)))
            References.Add(new RegionalWindowItem(window, OpenedPlan?.Definition.SeasonYear ?? DateTime.Now.Year));
    }

    private void EnsureChoice(PlanningChoiceFromViewModel? choice)
    {
        if (choice is not null && !Choices.Contains(choice)) Choices.Add(choice);
    }

    private void ApplyBatches(PlantingPlanDetailsFromViewModel plan)
    {
        var selectedId = SelectedBatch?.Id;
        _applying = true;
        try
        {
            Batches.Clear();
            foreach (var batch in plan.Batches) Batches.Add(new PlannedBatchListItem(batch));
            SelectedBatch = Batches.FirstOrDefault(x => x.Id == selectedId);
        }
        finally
        {
            _applying = false;
        }

        OnPropertyChanged(nameof(SeasonHarvestText));
    }

    private void UpdateSummary(PlantingPlanDetailsFromViewModel plan)
    {
        var current = Plans.FirstOrDefault(x => x.Id == plan.Id);
        var updated = Summary(plan);
        _applying = true;
        try
        {
            if (current is null) Plans.Add(updated);
            else Plans[Plans.IndexOf(current)] = updated;
            if (SelectedPlan?.Id == plan.Id) SelectedPlan = updated;
        }
        finally
        {
            _applying = false;
        }
    }

    private static PlantingPlanSummaryFromViewModel Summary(PlantingPlanDetailsFromViewModel plan) => new(plan.Id,
        plan.Version, plan.Definition.Name.Value, plan.Definition.SeasonYear, plan.Definition.RegionName);

    private static string Climate(Option<AnnualDateWindow> window, int year) => window.Match(
        value => value.Project(year).Match(PlanningPresentation.Window, error => error), () => "нет сведений");

    private int Error(string error)
    {
        Status = error;
        return 0;
    }
}
