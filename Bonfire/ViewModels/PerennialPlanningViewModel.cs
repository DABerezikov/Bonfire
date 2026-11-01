using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Application.Planning;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models.Planning;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;

namespace Bonfire.ViewModels;

public sealed class PerennialPlanningViewModel : ViewModel
{
    private readonly IPerennialPlanningService _service;
    private readonly IPlanningService _planning;
    private readonly IUserDialog _dialog;
    private readonly TimeProvider _time;
    private bool _applying;
    private long _seasonReadRevision;
    private ImmutableArray<PerennialSeasonFromViewModel> _seasons = [];
    public ObservableCollection<PerennialPlantingFromViewModel> Plantings { get; } = [];
    public ObservableCollection<PlantingPlanSummaryFromViewModel> Plans { get; } = [];
    public ObservableCollection<PlanningChoiceFromViewModel> Choices { get; } = [];
    public ObservableCollection<RegionFromViewModel> Regions { get; } = [];
    public PerennialEditorDraft Draft { get; private set => Set(ref field, value); }
    public PerennialSeasonDraft Season { get; private set => Set(ref field, value); } = new(new());
    public PerennialSeasonPreview? Preview { get; private set => Set(ref field, value); }
    public string PreviewText { get; private set => Set(ref field, value); } = "Выберите посадку и сезонный план";
    public string Status { get; private set => Set(ref field, value); } = "Межсезонные посадки";
    public bool IsBusy { get; private set => Set(ref field, value); }
    public string SavedCount => $"Сохранено в выбранном плане: {_seasons.Length}";
    public PerennialPlantingFromViewModel? Selected
    {
        get;
        set { if (!Set(ref field, value) || _applying || value is null) return; SetDraft(PerennialEditorDraft.Open(value)); EnsureOptions(); ResetSeason(); }
    }
    public PlantingPlanSummaryFromViewModel? SelectedPlan
    {
        get;
        set { if (Set(ref field, value) && !_applying) { Preview = null; _ = LoadSeasonsAsync(); } }
    }
    public PerennialPlanningViewModel(IPerennialPlanningService service, IPlanningService planning, IUserDialog dialog, TimeProvider time)
    {
        (_service, _planning, _dialog, _time) = (service, planning, dialog, time);
        Draft = new(time.GetLocalNow().Year); SetDraft(Draft); SetSeason(Season);
    }
    public ICommand LoadCommand => field ??= new LambdaCommandAsync(LoadAsync, () => !IsBusy);
    public ICommand NewCommand => field ??= new LambdaCommand(New, () => !IsBusy);
    public ICommand SaveCommand => field ??= new LambdaCommandAsync(SaveAsync, () => !IsBusy && Draft.IsDirty && Draft.Build().IsSuccess);
    public ICommand CancelCommand => field ??= new LambdaCommand(() => { if (Selected is { } selected) SetDraft(PerennialEditorDraft.Open(selected)); else New(); });
    public ICommand DeleteCommand => field ??= new LambdaCommandAsync(DeleteAsync, () => !IsBusy && Selected is not null);
    public ICommand CopyProfileCommand => field ??= new LambdaCommandAsync(CopyProfileAsync, () => !IsBusy && Draft.Choice is not null && Draft.Region is not null);
    public ICommand CalculateCommand => field ??= new LambdaCommandAsync(CalculateAsync, () => !IsBusy && Selected is not null && SelectedPlan is not null && !Draft.IsDirty && Season.Build().IsSuccess);
    public ICommand SaveSeasonCommand => field ??= new LambdaCommandAsync(SaveSeasonAsync, CanSaveSeason);
    public ICommand DeleteSeasonCommand => field ??= new LambdaCommandAsync(DeleteSeasonAsync, () => !IsBusy && SelectedPlan is not null && Selected is not null && _seasons.Any(x => x.PlantingId == Selected.Id));

    public async Task LoadAsync()
    {
        if (IsBusy) return; IsBusy = true;
        try
        {
            (await _service.ReadAsync()).Match(items =>
            {
                var id = Selected?.Id; _applying = true;
                try { Plantings.Clear(); foreach (var item in items) Plantings.Add(item); Selected = Plantings.FirstOrDefault(x => x.Id == id); }
                finally { _applying = false; }
                if (!Draft.IsDirty && Selected is { } selected) SetDraft(PerennialEditorDraft.Open(selected)); return 0;
            }, Error);
            (await _planning.ReadPlansAsync()).Match(items =>
            {
                var id = SelectedPlan?.Id; _applying = true;
                try { Plans.Clear(); foreach (var item in items) Plans.Add(item); SelectedPlan = Plans.FirstOrDefault(x => x.Id == id); }
                finally { _applying = false; } return 0;
            }, Error);
            var choices = await _planning.ReadChoicesAsync(); var choice = Draft.Choice;
            choices.Match(items => { Choices.Clear(); foreach (var item in items) Choices.Add(item); Draft.Choice = choice; return 0; }, Error);
            var regions = await _planning.ReadRegionsAsync(); var region = Draft.Region;
            regions.Match(items => { Regions.Clear(); foreach (var item in items) Regions.Add(item); Draft.Region = region; return 0; }, Error);
            EnsureOptions(); await LoadSeasonsAsync(true);
        }
        finally { IsBusy = false; CommandManager.InvalidateRequerySuggested(); }
    }
    public async Task SaveAsync()
    {
        if (IsBusy) return; var draft = Draft; var values = draft.Values; var definition = draft.Build();
        if (definition.IsFailure) { definition.Match(_ => 0, Error); return; } IsBusy = true;
        try { (await _service.SaveAsync(draft.Id, draft.Version, definition.ValueOrThrow())).Match(saved =>
        {
            _applying = true;
            try { var previous = Plantings.FirstOrDefault(x => x.Id == saved.Id); if (previous is not null) Plantings[Plantings.IndexOf(previous)] = saved; else Plantings.Add(saved);
                if (ReferenceEquals(Draft, draft)) { draft.Accept(values, saved); Selected = saved; } }
            finally { _applying = false; }
            Status = "Посадка сохранена; межсезонный ID не меняется"; return 0;
        }, Error); }
        finally { IsBusy = false; CommandManager.InvalidateRequerySuggested(); }
    }
    private async Task CopyProfileAsync()
    {
        var draft = Draft; var values = draft.Values;
        if (values.Choice is not { } choice || values.Region is not { } region) return;
        var result = await _planning.ReadContextAsync(region.Id.Value, choice.CultureId, choice.SortId);
        if (!ReferenceEquals(Draft, draft) || draft.Values != values) return;
        result.Match(context => GrowingProfileResolver.ResolveExtras(context.CultureProfile.Match(x => x.Extras, () => new()),
            context.SortSettings.Match(x => x.Extras, () => new()), new()).LifeCycle.Match(profile =>
            { draft.CopyProfile(profile); Status = "Скопирован профиль из " + PlanningPresentation.Source(profile.Source); return 0; },
            () => Error("В справочнике жизненный цикл не заполнен")), Error);
    }
    public async Task CalculateAsync()
    {
        if (IsBusy || Selected is not { } planting || SelectedPlan is not { } plan || Draft.IsDirty) return;
        var season = Season; var values = season.Values; var manual = season.Build();
        if (manual.IsFailure) { manual.Match(_ => 0, Error); return; } IsBusy = true;
        try { var result = await _service.CalculateAsync(plan.Id, planting.Id, manual.ValueOrThrow());
            if (Selected?.Id != planting.Id || SelectedPlan?.Id != plan.Id || !ReferenceEquals(Season, season) || season.Values != values || Draft.IsDirty) return;
            result.Match(preview => { Preview = preview; PreviewText = Describe(preview.Snapshot, "Расчёт, ещё не сохранён"); return 0; }, Error); }
        finally { IsBusy = false; CommandManager.InvalidateRequerySuggested(); }
    }
    public async Task SaveSeasonAsync()
    {
        if (!CanSaveSeason() || Preview is not { } preview) return;
        var season = Season; var values = season.Values; IsBusy = true;
        try { (await _service.SaveSeasonAsync(preview)).Match(saved =>
        {
            UpdatePlan(preview.PlanId, saved.PlanVersion);
            if (SelectedPlan?.Id == preview.PlanId)
            { _seasons = _seasons.Where(x => x.PlantingId != preview.PlantingId).Append(saved.Season).ToImmutableArray(); OnPropertyChanged(nameof(SavedCount)); }
            if (ReferenceEquals(Season, season)) { season.Accept(values); if (season.Values == values) { Preview = null; PreviewText = Describe(saved.Season.Snapshot, "Сохранённый снимок"); } }
            Status = "Сезонное представление сохранено; новый посев не создавался"; return 0;
        }, Error); }
        finally { IsBusy = false; CommandManager.InvalidateRequerySuggested(); }
    }
    private bool CanSaveSeason() => !IsBusy && !Draft.IsDirty && Preview is { } preview && Selected?.Id == preview.PlantingId &&
        Selected.Version == preview.PlantingVersion && SelectedPlan?.Id == preview.PlanId && SelectedPlan.Version == preview.PlanVersion &&
        Season.Build().Match(manual => manual == preview.Snapshot.Manual, _ => false);
    private async Task DeleteSeasonAsync()
    {
        var plan = SelectedPlan; var planting = Selected; if (plan is null || planting is null) return; IsBusy = true;
        try { (await _service.DeleteSeasonAsync(plan.Id, plan.Version, planting.Id)).Match(version =>
        { UpdatePlan(plan.Id, version); if (SelectedPlan?.Id == plan.Id) { _seasons = _seasons.Where(x => x.PlantingId != planting.Id).ToImmutableArray(); OnPropertyChanged(nameof(SavedCount)); }
            if (SelectedPlan?.Id == plan.Id && Selected?.Id == planting.Id) { Preview = null; PreviewText = "Сезонное представление удалено; посадка и сборы сохранены"; } return 0; }, Error); }
        finally { IsBusy = false; }
    }
    private async Task DeleteAsync()
    {
        var selected = Selected; var draft = Draft;
        if (selected is null || !_dialog.YesNoQuestion("Удалить межсезонную посадку и её сезонные представления? Сборы урожая сохранятся.", "Межсезонные посадки")) return;
        IsBusy = true;
        try { (await _service.DeleteAsync(selected.Id, selected.Version)).Match(id =>
        { _applying = true; try { var item = Plantings.FirstOrDefault(x => x.Id == id); if (item is not null) Plantings.Remove(item); } finally { _applying = false; }
            _seasons = _seasons.Where(x => x.PlantingId != id).ToImmutableArray(); OnPropertyChanged(nameof(SavedCount)); if (ReferenceEquals(Draft, draft)) New(); return 0; }, Error); }
        finally { IsBusy = false; }
    }
    private async Task LoadSeasonsAsync(bool preserveDirty = false)
    {
        var plan = SelectedPlan; var revision = ++_seasonReadRevision;
        if (plan is null) { _seasons = []; OnPropertyChanged(nameof(SavedCount)); ResetSeason(); return; }
        var result = await _service.ReadSeasonsAsync(plan.Id);
        if (revision != _seasonReadRevision || SelectedPlan?.Id != plan.Id) return;
        result.Match(items => { _seasons = items; OnPropertyChanged(nameof(SavedCount)); if (!preserveDirty || Season.Values == Season.Original) ResetSeason(); return 0; }, Error);
    }
    private void ResetSeason()
    {
        var saved = _seasons.FirstOrDefault(x => x.PlantingId == Selected?.Id);
        SetSeason(new(saved?.Snapshot.Manual ?? new())); Preview = null;
        PreviewText = saved is null ? "В плане пока нет прогноза этой посадки" : Describe(saved.Snapshot, "Сохранённый снимок");
    }
    private void New()
    { _applying = true; try { Selected = null; } finally { _applying = false; } SetDraft(new(_time.GetLocalNow().Year)); ResetSeason(); }
    private void SetDraft(PerennialEditorDraft draft)
    { Draft = draft; draft.PropertyChanged += (sender, args) => { if (ReferenceEquals(sender, Draft) && args.PropertyName == nameof(PerennialEditorDraft.Values)) { Preview = null; CommandManager.InvalidateRequerySuggested(); } }; }
    private void SetSeason(PerennialSeasonDraft season)
    { Season = season; season.PropertyChanged += (sender, args) => { if (ReferenceEquals(sender, Season) && args.PropertyName == nameof(PerennialSeasonDraft.Values)) { Preview = null; PreviewText = "Ручные периоды изменены — выполните расчёт"; CommandManager.InvalidateRequerySuggested(); } }; }
    private void EnsureOptions()
    { if (Draft.Choice is { } choice && !Choices.Contains(choice)) Choices.Add(choice); if (Draft.Region is { } region && !Regions.Contains(region)) Regions.Add(region); }
    private void UpdatePlan(int id, int version)
    { var item = Plans.FirstOrDefault(x => x.Id == id); if (item is null) return; var selectedId = SelectedPlan?.Id; _applying = true;
        try { var updated = item with { Version = version }; Plans[Plans.IndexOf(item)] = updated; if (selectedId == id) SelectedPlan = updated; } finally { _applying = false; } }
    private static string Describe(PerennialSeasonSnapshot snapshot, string title) => $"{title}; {snapshot.Projection.SeasonYear}, сезон жизни №{snapshot.Projection.AgeSeason}\n"
        + PlanningPresentation.Period("Цветение", snapshot.Projection.Periods.Flowering) + "\n" + PlanningPresentation.Period("Плодоношение", snapshot.Projection.Periods.Fruiting);
    private int Error(string error) { Status = error; return 0; }
}
