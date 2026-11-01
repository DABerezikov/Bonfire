using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Application.Harvest;
using Bonfire.Application.Planning;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Harvest;
using Bonfire.Domain.Planning;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models.Planning;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;

namespace Bonfire.ViewModels;

public sealed record HarvestJournalItem(HarvestEntryFromViewModel Snapshot)
{
    public string Label => $"{PlanningInputParser.Format(Snapshot.Entry.Date)} · {Snapshot.Entry.Mass.Kg} кг · {Snapshot.Entry.Identity.CultureName} / {Snapshot.Entry.Identity.SortName}";
    public string Owner => $"Сезон {Snapshot.Entry.Identity.SeasonYear}" + Snapshot.Entry.Identity.BatchId.Match(id => $"; партия #{id.Value}",
        () => Snapshot.Entry.Identity.PerennialId.Match(id => $"; многолетняя #{id.Value}", () => "; без партии"));
}

public sealed class HarvestJournalViewModel(IHarvestService service, IPlanningService planning, IUserDialog dialog, TimeProvider time) : ViewModel
{
    private ImmutableArray<HarvestEntryFromViewModel> _entries = [];
    private bool _applying;
    private long _dataRevision;
    public ObservableCollection<HarvestJournalItem> Entries { get; } = [];
    public ObservableCollection<HarvestSourceFromViewModel> Sources { get; } = [];
    public ObservableCollection<PlanningChoiceFromViewModel> Choices { get; } = [];
    public ObservableCollection<RegionFromViewModel> Regions { get; } = [];
    public HarvestJournalDraft Draft { get; private set => Set(ref field, value); } = new(time);
    public bool IsBusy { get; private set => Set(ref field, value); }
    public bool CanChooseIdentity => !IsBusy && Draft.IsNew;
    public string Status { get; private set => Set(ref field, value); } = "Ручной журнал сборов";
    public string Summary { get; private set => Set(ref field, value); } = "";
    public string FilterSeason { get; set { if (Set(ref field, value)) ApplyFilters(); } } = "";
    public string FilterCulture { get; set { if (Set(ref field, value)) ApplyFilters(); } } = "";
    public string FilterSort { get; set { if (Set(ref field, value)) ApplyFilters(); } } = "";
    public string FilterBatch { get; set { if (Set(ref field, value)) ApplyFilters(); } } = "";
    public string FilterPerennial { get; set { if (Set(ref field, value)) ApplyFilters(); } } = "";
    public HarvestJournalItem? Selected
    {
        get;
        set { if (!Set(ref field, value) || _applying || value is null) return; Draft = HarvestJournalDraft.Open(value.Snapshot, time); OnPropertyChanged(nameof(CanChooseIdentity)); }
    }
    public ICommand LoadCommand => field ??= new LambdaCommandAsync(LoadAsync, () => !IsBusy);
    public ICommand SourcesCommand => field ??= new LambdaCommandAsync(LoadSourcesAsync, () => !IsBusy);
    public ICommand NewCommand => field ??= new LambdaCommand(New, () => !IsBusy);
    public ICommand NoSourceCommand => field ??= new LambdaCommand(() => Draft.Source = null, () => CanChooseIdentity);
    public ICommand CancelCommand => field ??= new LambdaCommand(() => { if (Selected is { } selected) Draft = HarvestJournalDraft.Open(selected.Snapshot, time); else New(); OnPropertyChanged(nameof(CanChooseIdentity)); });
    public ICommand SaveCommand => field ??= new LambdaCommandAsync(SaveAsync, () => !IsBusy && Draft.IsDirty && Draft.Build().IsSuccess);
    public ICommand DeleteCommand => field ??= new LambdaCommandAsync(DeleteAsync, () => !IsBusy && Selected is not null);

    public async Task LoadAsync()
    {
        var revision = _dataRevision;
        var read = await service.ReadAsync();
        if (revision == _dataRevision) read.Match(items => { _entries = items; ApplyFilters(); return 0; }, Error);
        var choices = await planning.ReadChoicesAsync(); var choice = Draft.Choice;
        choices.Match(items => { Choices.Clear(); foreach (var item in items) Choices.Add(item);
            if (choice is not null && !Choices.Contains(choice)) Choices.Add(choice); Draft.Choice = choice; return 0; }, Error);
        var regions = await planning.ReadRegionsAsync(); var region = Draft.Region;
        regions.Match(items => { Regions.Clear(); foreach (var item in items) Regions.Add(item);
            if (region is not null && !Regions.Contains(region)) Regions.Add(region); Draft.Region = region ?? Regions.FirstOrDefault(); return 0; }, Error);
        await LoadSourcesAsync();
    }
    public async Task LoadSourcesAsync()
    {
        if (!int.TryParse(Draft.Season, out var year)) { Status = "Укажите сезон для списка партий"; return; }
        var draft = Draft;
        var result = await service.ReadSourcesAsync(year);
        if (!ReferenceEquals(Draft, draft) || !int.TryParse(Draft.Season, out var currentYear) || currentYear != year) return;
        var source = Draft.Source;
        result.Match(items => { Sources.Clear(); foreach (var item in items) Sources.Add(item);
            if (source is not null && !Sources.Contains(source)) Sources.Add(source); Draft.Source = source; ApplyFilters(); return 0; }, Error);
    }
    public async Task SaveAsync()
    {
        if (IsBusy) return;
        var draft = Draft; var submitted = draft.Values; var input = draft.Build();
        if (input.IsFailure) { input.Match(_ => 0, Error); return; }
        IsBusy = true; OnPropertyChanged(nameof(CanChooseIdentity));
        try
        {
            (await service.SaveAsync(input.ValueOrThrow())).Match(saved =>
            {
                _entries = _entries.Where(x => x.Id != saved.Id).Append(saved).ToImmutableArray();
                _dataRevision++;
                ApplyFilters();
                if (ReferenceEquals(Draft, draft))
                {
                    draft.Accept(submitted, saved); _applying = true;
                    try { Selected = Entries.FirstOrDefault(x => x.Snapshot.Id == saved.Id); } finally { _applying = false; }
                }
                Status = "Сбор сохранён"; return 0;
            }, Error);
        }
        finally { IsBusy = false; OnPropertyChanged(nameof(CanChooseIdentity)); CommandManager.InvalidateRequerySuggested(); }
    }
    private async Task DeleteAsync()
    {
        var selected = Selected;
        if (selected is null || !dialog.YesNoQuestion("Удалить выбранный сбор из журнала?", "Сбор урожая")) return;
        var draft = Draft; IsBusy = true;
        try { (await service.DeleteAsync(selected.Snapshot.Id, selected.Snapshot.Version)).Match(id =>
        { _entries = _entries.Where(x => x.Id != id).ToImmutableArray(); _dataRevision++; ApplyFilters(); if (ReferenceEquals(Draft, draft)) New(); Status = "Сбор удалён"; return 0; }, Error); }
        finally { IsBusy = false; OnPropertyChanged(nameof(CanChooseIdentity)); }
    }
    private void New()
    {
        var region = Draft.Region;
        _applying = true; try { Selected = null; } finally { _applying = false; }
        Draft = new(time) { Region = region };
        if (region is not null) Draft.Date = PlanningInputParser.Format(DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time.GetUtcNow(), region.Definition.TimeZone).DateTime));
        OnPropertyChanged(nameof(CanChooseIdentity));
    }
    private void ApplyFilters()
    {
        int? year = null, batch = null, perennial = null;
        if (FilterSeason.Length > 0) { if (!int.TryParse(FilterSeason, out var value) || value is < 1 or > 9999) { Summary = "Неверный сезон фильтра"; return; } year = value; }
        if (FilterBatch.Length > 0) { if (!int.TryParse(FilterBatch, out var value) || value <= 0) { Summary = "Неверный ID партии"; return; } batch = value; }
        if (FilterPerennial.Length > 0) { if (!int.TryParse(FilterPerennial, out var value) || value <= 0) { Summary = "Неверный ID межсезонной посадки"; return; } perennial = value; }
        var filtered = _entries.Where(x => (!year.HasValue || x.Entry.Identity.SeasonYear == year) &&
            x.Entry.Identity.CultureName.Contains(FilterCulture, StringComparison.CurrentCultureIgnoreCase) &&
            x.Entry.Identity.SortName.Contains(FilterSort, StringComparison.CurrentCultureIgnoreCase) &&
            (!batch.HasValue || x.Entry.Identity.BatchId.Match(id => id.Value == batch, () => false)) &&
            (!perennial.HasValue || x.Entry.Identity.PerennialId.Match(id => id.Value == perennial, () => false))).DistinctBy(x => x.Id).ToArray();
        var selectedId = Selected?.Snapshot.Id; _applying = true;
        try { Entries.Clear(); foreach (var entry in filtered.OrderByDescending(x => x.Entry.Date).ThenBy(x => x.Id)) Entries.Add(new(entry)); Selected = Entries.FirstOrDefault(x => x.Snapshot.Id == selectedId); }
        finally { _applying = false; }
        Summary = HarvestSummary.Calculate(filtered.Select(x => new RecordedHarvest(HarvestEntryId.Create(x.Id).ValueOrThrow(), x.Entry))).Match(total =>
            $"Сборов: {total.Count}; собрано {total.TotalKg:0.###} кг; первый сбор: {total.FirstDate.Match(PlanningInputParser.Format, () => "нет")}" + Compare(total.TotalKg, batch), error => error);
    }
    private string Compare(decimal total, int? batch)
    {
        if (!batch.HasValue) return ". Сравнение с прогнозом доступно при фильтре одной партии.";
        if (FilterPerennial.Length > 0) return ". Фильтр межсезонной посадки исключает партии; сравнение недоступно.";
        var source = Sources.FirstOrDefault(x => x.Identity.BatchId.Match(id => id.Value == batch, () => false));
        if (source is null || int.TryParse(FilterSeason, out var year) && source.Identity.SeasonYear != year) return ". Сохранённый прогноз партии недоступен.";
        if (!source.Identity.CultureName.Contains(FilterCulture, StringComparison.CurrentCultureIgnoreCase) ||
            !source.Identity.SortName.Contains(FilterSort, StringComparison.CurrentCultureIgnoreCase)) return ". Фильтры исключают выбранную партию; сравнение недоступно.";
        try { return source.ForecastPerPlantKg.Match(x => $"; прогноз по растениям {x:0.###} кг, отклонение {checked(total - x):+0.###;-0.###;0} кг", () => "; прогноз по растениям неизвестен")
            + source.ForecastPerM2Kg.Match(x => $"; прогноз по площади {x:0.###} кг, отклонение {checked(total - x):+0.###;-0.###;0} кг", () => "; прогноз по площади неизвестен"); }
        catch (OverflowException) { return "; отклонение превышает диапазон"; }
    }
    private int Error(string error) { Status = error; return 0; }
}
