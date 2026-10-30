using System.Collections.Immutable;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Bonfire.Data;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models;
using Bonfire.Models.Mappers;
using Bonfire.Services.Extensions;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;
using Bonfire.Application.Libraries;

namespace Bonfire.ViewModels;

public class SeedsViewModel : SourceSelectionViewModel
{
    private readonly ISeedsService _seedsService;
    private readonly IUserDialog _userDialog;
    private readonly IReportService _reportService;
    private bool _ignoreDiscard;
    private bool _isSaving;

    public SortEditModel? SeedSortDetail
    {
        get;
        private set => Set(ref field, value);
    }

    public int EditedProducerId
    {
        get;
        set => Set(ref field, value);
    }

    public SeedEditorOptionsFromViewModel EditorOptions
    {
        get;
        private set => Set(ref field, value);
    } = new([], [], []);

    public bool EditorHasBindingErrors { get; set; }
    public bool CanEditSeedDetails => !_isSaving;

    public bool HasUnsavedSeedChanges => EditedItem is { } draft &&
        (EditorHasBindingErrors || SeedSortDetail is { IsDirty: true }
         || EditedProducerId != draft.OriginalProducerId
         || draft.HasPackageChanges);

    public bool TryDiscardSeedChanges()
    {
        if (_isSaving) return false;
        if (HasUnsavedSeedChanges && !_userDialog.YesNoQuestion(
                "Сбросить несохранённые изменения семян и характеристик сорта?", "Несохранённые изменения"))
            return false;
        LoadSeedDraft(SelectedItem);
        return true;
    }

    public SeedsViewModel(ISeedsService seedsService, IUserDialog userDialog, IReportService reportService)
    {
        _seedsService = seedsService;
        _userDialog = userDialog;
        _reportService = reportService;
        _seedsView = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(SeedsFromViewModel.Culture), ListSortDirection.Ascending),
                new SortDescription(nameof(SeedsFromViewModel.Sort), ListSortDirection.Ascending),
                new SortDescription(nameof(SeedsFromViewModel.Producer), ListSortDirection.Ascending)
            }
        };
        _seedsView.Filter += _SeedsViewSource_Filter;

        _cultureListView = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(CultureFromViewModel.Name), ListSortDirection.Ascending)
            }
        };
        _cultureListView.Filter += _CultureListView_Filter;

        _sortListView = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(SortFromSeedsViewModel.Name), ListSortDirection.Ascending)
            }
        };
        _sortListView.Filter += _SortListView_Filter;

        _producerListView = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(ProducerFromViewModel.Name), ListSortDirection.Ascending)
            }
        };
        _producerListView.Filter += _ProducerListView_Filter;
    }

    public bool IsActive
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            // Семена могли быть списаны в планировщике огорода (другой контекст) —
            // перечитываем из БД при активации вкладки, чтобы остаток был актуален.
            if (value && Seeds != null)
                _ = RefreshSeedsAsync();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private async Task<Result<ImmutableArray<SeedReadFromViewModel>>> RefreshSeedsAsync()
    {
        var result = await _seedsService.ReadSeedsAsync();
        result.Match(seeds =>
        {
            Seeds = new(seeds);
            UpdateCollectionViewSource();
            return true;
        }, error => { _userDialog.Error(error); return false; });
        return result;
    }

    // Фильтрация главного списка

    public ICollectionView? SeedsView => _seedsView.View;
    private readonly CollectionViewSource _seedsView;

    public string SeedFilter
    {
        get;
        set
        {
            if (field != value && !TryDiscardSeedChanges()) return;
            if (Set(ref field, value))
            {
                SelectedItem = null;
                SeedsView?.Refresh();
            }
        }
    } = "-Выбрать все-";

    private void _SeedsViewSource_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not SeedsFromViewModel seed) return;
        if (SeedFilter != "-Выбрать все-")
        {
            if (IsHaving)
            {
                if (seed.Culture != null && (!seed.Culture.Contains(SeedFilter, StringComparison.OrdinalIgnoreCase) ||
                                             (seed.AmountSeedsQuantity == null && seed.AmountSeedsWeight == null)))
                    e.Accepted = false;
            }
            else
            {
                if (seed.Culture != null && !seed.Culture.Contains(SeedFilter, StringComparison.OrdinalIgnoreCase))
                    e.Accepted = false;
            }
        }
        else
        {
            if (!IsHaving) return;
            if (seed.AmountSeedsQuantity == null && seed.AmountSeedsWeight == null)
                e.Accepted = false;
        }
    }

    // Основные данные

    public ObservableCollection<SeedReadFromViewModel>? Seeds
    {
        get;
        set => Set(ref field, value);
    }

    public SeedsFromViewModel? SelectedSeedsViewItem
    {
        get;
        set
        {
            if (field?.Id != value?.Id && !_ignoreDiscard && !TryDiscardSeedChanges())
            {
                OnPropertyChanged();
                return;
            }
            Set(ref field, value);
            SelectedItem = value != null ? Seeds!.First(s => s.Id == value.Id) : null;
        }
    }

    private SeedReadFromViewModel? _selectedItem;
    public SeedReadFromViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (!ReferenceEquals(_selectedItem, value) && !_ignoreDiscard && !TryDiscardSeedChanges()) return;
            if (Set(ref _selectedItem, value))
            {
                LoadSeedDraft(value);
            }
        }
    }

    public SeedEditorDraft? EditedItem
    {
        get;
        private set => Set(ref field, value);
    }

    public List<string> ListCulture
    {
        get;
        set => Set(ref field, value);
    } = ["-Выбрать все-"];

    public bool IsHaving
    {
        get;
        set
        {
            Set(ref field, value);
            SelectedItem = null;
            SeedsView?.Refresh();
        }
    } = true;

    public string AddQuantityInPac
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    public string AddQuantityPac
    {
        get;
        set => Set(ref field, value);
    } = "1";

    public DateTime AddBestBy
    {
        get;
        set => Set(ref field, value);
    } = new DateTime(DateTime.Now.Year + 1, 12, 31);

    public string AddCostPack
    {
        get;
        set => Set(ref field, value);
    } = "0";

    // Источник семян (радио-кнопки) — общая часть в SourceSelectionViewModel

    internal string SeedSource
    {
        get => SourceValue;
        set => SourceValue = value;
    }

    protected override void OnSourceChanged() => OnPropertyChanged(nameof(IsCollected));

    public bool IsCollected
    {
        get => SourceValue == PlantSources.Collected;
        set { if (value) { SourceValue = PlantSources.Collected; AddProducer = Producers.Own; } }
    }

    // Примечание

    public string AddNote
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    // Единицы измерения

    public List<string> AddSizeList
    {
        get;
        set => Set(ref field, value);
    } = [Units.GramsOption, Units.PiecesOption];

    public string AddSize
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    // Класс растения

    public ObservableCollection<string> AddClassList
    {
        get;
        set => Set(ref field, value);
    } = new(PlantClassList.GetClassList());

    public string AddClass
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    // Выбор культуры

    public ICollectionView? CultureListView => _cultureListView.View;
    private readonly CollectionViewSource _cultureListView;

    private void _CultureListView_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not CultureFromViewModel culture || string.IsNullOrEmpty(AddCulture)) return;
        if (culture.Name != null && !culture.Name.Contains(AddCulture, StringComparison.OrdinalIgnoreCase))
            e.Accepted = false;
    }

    public ObservableCollection<CultureFromViewModel> AddCultureList
    {
        get;
        set => Set(ref field, value);
    } = [];

    public string AddCulture
    {
        get;
        set
        {
            if (Set(ref field, value))
                CultureListView?.Refresh();
        }
    } = string.Empty;

    // Выбор сорта

    public ICollectionView SortListView => _sortListView.View;
    private readonly CollectionViewSource _sortListView;

    private void _SortListView_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not SortFromSeedsViewModel sort || string.IsNullOrEmpty(AddSort)) return;
        if (sort.Name != null && !sort.Name.Contains(AddSort, StringComparison.OrdinalIgnoreCase))
            e.Accepted = false;
    }

    public ObservableCollection<SortFromSeedsViewModel> AddSortList
    {
        get;
        set => Set(ref field, value);
    } = [];

    public string AddSort
    {
        get;
        set
        {
            if (Set(ref field, value))
                SortListView?.Refresh();
        }
    } = string.Empty;

    // Выбор производителя

    public ICollectionView ProducerListView => _producerListView.View;
    private readonly CollectionViewSource _producerListView;

    private void _ProducerListView_Filter(object sender, FilterEventArgs e)
    {
        if (!(e.Item is ProducerFromViewModel producer) || string.IsNullOrEmpty(AddProducer)) return;
        if (producer.Name != null && !producer.Name.Contains(AddProducer, StringComparison.OrdinalIgnoreCase))
            e.Accepted = false;
    }

    public ObservableCollection<ProducerFromViewModel> AddProducerList
    {
        get;
        set => Set(ref field, value);
    } = [];

    public string AddProducer
    {
        get;
        set
        {
            if (Set(ref field, value))
            {
                ProducerListView?.Refresh();
                if (field == Producers.Own)
                    IsCollected = true;
            }
        }
    } = string.Empty;

    // Методы

    // Списки культур/сортов/производителей строятся из уже загруженной коллекции Seeds.
    private void LoadListCulture()
    {
        var listCulture = Seeds!
            .Select(s => s.Plant.PlantCulture.Name)
            .Distinct()
            .OrderBy(s => s);

        var addListCulture = Seeds!
            .Select(s => new CultureFromViewModel
            {
                Id = s.Plant.PlantCulture.Id,
                Name = s.Plant.PlantCulture.Name
            })
            .Distinct(s => s!.Name)
            .OrderBy(s => s.Name);

        ListCulture.AddRange(listCulture);
        AddCultureList.AddRange(addListCulture.ToList());
        _cultureListView.Source = AddCultureList;
        OnPropertyChanged(nameof(CultureListView));
    }

    private void LoadListSort()
    {
        var addListSort = Seeds!
            .Select(s => new SortFromSeedsViewModel
            {
                Id = s.Plant.PlantSort.Id,
                Name = s.Plant.PlantSort.Name
            })
            .Distinct(s => s!.Name)
            .OrderBy(s => s.Name);
        AddSortList.AddRange(addListSort.ToList());
        _sortListView.Source = AddSortList;
        OnPropertyChanged(nameof(SortListView));
    }

    private void LoadListProducer()
    {
        var addListProducer = Seeds!
            .Select(s => new ProducerFromViewModel
            {
                Id = s.Plant.PlantSort.Producer.Id,
                Name = s.Plant.PlantSort.Producer.Name
            })
            .Distinct(s => s!.Name)
            .OrderBy(s => s.Name);
        AddProducerList.AddRange(addListProducer.ToList());
        _producerListView.Source = AddProducerList;
        OnPropertyChanged(nameof(ProducerListView));
    }

    private bool Verification()
    {
        return AddBestBy != default
               && AddClass != string.Empty
               && AddCostPack != string.Empty
               && AddCulture != string.Empty
               && AddProducer != string.Empty
               && AddQuantityInPac != string.Empty
               && AddSort != string.Empty
               && SeedSource != string.Empty;
    }

    private void ClearFieldSeedView()
    {
        AddBestBy = new DateTime(DateTime.Now.Year + 1, 12, 31);
        AddClass = "Овощи";
        AddCostPack = "0";
        AddCulture = string.Empty;
        AddProducer = string.Empty;
        AddQuantityInPac = string.Empty;
        AddQuantityPac = "1";
        AddSort = string.Empty;
        IsSold = true;
        AddNote = string.Empty;
    }

    internal void UpdateCollectionViewSource(int id = -1)
    {
        var newCollection = Seeds!.Select(SeedMapper.ToViewModel).SortSeeds();
        var collection = newCollection.ToArray();
        _seedsView.Source = collection;
        if (id != -1)
        {
            var current = collection.FirstOrDefault(s => s.Id == id);
            _seedsView.View.MoveCurrentTo(current);
        }
        OnPropertyChanged(nameof(SeedsView));
        OnPropertyChanged(nameof(ListCulture));
    }

    private void LoadSeedDraft(SeedReadFromViewModel? seed)
    {
        EditedItem = seed is { } source ? SeedEditMapper.FromSeed(source) : null;
        SeedSortDetail = seed is { } selected ? LibraryMapper.ToEditModel(selected.Plant.PlantSort) : null;
        EditedProducerId = EditedItem is { } draft ? draft.OriginalProducerId : 0;
        EditorHasBindingErrors = false;
        OnPropertyChanged(nameof(SelectedItem));
    }

    private void RebuildSeedSuggestions()
    {
        AddCultureList.Clear(); AddSortList.Clear(); AddProducerList.Clear();
        ListCulture = ["-Выбрать все-"];
        LoadListCulture(); LoadListSort(); LoadListProducer();
    }

    private void CreateSeedReport()
    {
        var items = Seeds!.Select(s => new SeedReportItem(
            s.Plant.PlantCulture.Name,
            s.Plant.PlantSort.Name,
            s.Plant.PlantSort.Producer.Name,
            s.SeedsInfo.ExpirationDate.ToShortDateString(),
            s.SeedsInfo.WeightPack,
            s.SeedsInfo.QuantityPack))
            .OrderBy(s => s.Culture);
        _reportService.CreateSeedsReport(items);
    }

    private void CreateFilteredSeedReport()
    {
        var items = SeedsView!
            .Cast<SeedsFromViewModel>()
            .Select(s => new SeedReportItem(
                s.Culture ?? string.Empty,
                s.Sort ?? string.Empty,
                s.Producer ?? string.Empty,
                s.ExpirationDate.ToShortDateString(),
                s.WeightPack ?? 0,
                s.QuantityPack ?? 0))
            .OrderBy(s => s.Culture);
        _reportService.CreateSeedsReport(items, "Семена_выборка");
    }

    private void ReplaceCachedSeed(int index, SeedReadFromViewModel updated)
    {
        var previous = Seeds![index];
        Seeds[index] = updated;
        if (!ReferenceEquals(_selectedItem, previous)) return;
        _selectedItem = updated;
        OnPropertyChanged(nameof(SelectedItem));
    }

    internal void SyncAfterSortUpdate(SortDetailsFromViewModel updated)
    {
        if (Seeds is { } seeds)
            for (var index = 0; index < seeds.Count; index++)
            {
                var seed = seeds[index];
                if (seed.Plant.PlantSort.Id != updated.Id) continue;
                var link = updated.Plants.FirstOrDefault(p => p.PlantId == seed.Plant.Id);
                ReplaceCachedSeed(index, seed with { Plant = seed.Plant with
                {
                    PlantSort = seed.Plant.PlantSort with { Name = updated.Name },
                    PlantCulture = link is not null ? link.Culture : seed.Plant.PlantCulture
                } });
            }

        var sortItem = AddSortList.FirstOrDefault(s => s.Id == updated.Id);
        if (sortItem is not null)
            AddSortList[AddSortList.IndexOf(sortItem)] =
                new SortFromSeedsViewModel { Id = updated.Id, Name = updated.Name };

        UpdateCollectionViewSource();
    }

    internal void SyncAfterCultureUpdate(Bonfire.Application.Libraries.CultureDetailsFromViewModel updated)
    {
        if (Seeds is { } seeds)
            for (var index = 0; index < seeds.Count; index++)
                if (seeds[index] is { } seed && seed.Plant.PlantCulture.Id == updated.Id)
                    ReplaceCachedSeed(index, seed with { Plant = seed.Plant with { PlantCulture = updated } });

        var cultureItem = AddCultureList.FirstOrDefault(c => c.Id == updated.Id);
        if (cultureItem is not null)
            AddCultureList[AddCultureList.IndexOf(cultureItem)] =
                new CultureFromViewModel { Id = updated.Id, Name = updated.Name };

        UpdateCollectionViewSource();
    }

    internal void SyncAfterProducerUpdate(Bonfire.Application.Libraries.ProducerDetailsFromViewModel updated)
    {
        if (Seeds is { } seeds)
            for (var index = 0; index < seeds.Count; index++)
                if (seeds[index] is { } seed && seed.Plant.PlantSort.Producer.Id == updated.Id)
                    ReplaceCachedSeed(index, seed with { Plant = seed.Plant with { PlantSort = seed.Plant.PlantSort with { Producer = updated } } });

        var producerItem = AddProducerList.FirstOrDefault(p => p.Id == updated.Id);
        if (producerItem is not null)
            AddProducerList[AddProducerList.IndexOf(producerItem)] =
                new ProducerFromViewModel { Id = updated.Id, Name = updated.Name };

        UpdateCollectionViewSource();
    }


    // Команды

    public ICommand LoadDataCommand => field
        ??= new LambdaCommandAsync(OnLoadDataCommandExecuted);

    private async Task<bool> ReloadEditorOptionsAsync()
        => (await _seedsService.GetEditorOptionsAsync()).Match(options => { EditorOptions = options; return true; },
            error => { _userDialog.Error(error); return false; });

    private async Task OnLoadDataCommandExecuted()
    {
        if (!await ReloadEditorOptionsAsync()) return;
        if (Seeds != null)
        {
            UpdateCollectionViewSource();
            return;
        }
        if ((await RefreshSeedsAsync()).IsFailure) return;
        LoadListCulture();
        LoadListSort();
        LoadListProducer();
    }

    public ICommand SeedsChoiceClassCommand => field
        ??= new LambdaCommandAsync(OnSeedsChoiceClassCommandExecuted);

    private async Task OnSeedsChoiceClassCommandExecuted(object p)
    {
        var filteredSeeds = p != null && p.ToString() != "Выбрать все"
            ? Seeds!.Where(seeds => seeds.Plant.PlantCulture.Class == p.ToString())
            : Seeds!;

        _seedsView.Source = filteredSeeds.Select(SeedMapper.ToViewModel).SortSeeds().ToArray();
        OnPropertyChanged(nameof(SeedsView));
    }

    public ICommand AddOrCorrectSeedCommand => field
        ??= new LambdaCommandAsync(OnAddOrCorrectSeedCommandExecuted, () => !_isSaving && Verification());

    private async Task OnAddOrCorrectSeedCommandExecuted()
    {
        if (!TryDiscardSeedChanges()) return;
        var request = new AddSeedRequest(
            AddCulture, AddSort, AddProducer, AddClass,
            SeedSource, AddSize,
            AddQuantityInPac.DoubleParseAdvanced(),
            AddQuantityPac.DoubleParseAdvanced(),
            AddCostPack.DecimalParseAdvanced(),
            AddBestBy, AddNote);

        SetSaving(true);
        try
        {
            var result = await _seedsService.AddOrUpdateSeedAsync(request);
            await result.Match(async receipt =>
            {
                _ignoreDiscard = true;
                if ((await RefreshSeedsAsync()).IsFailure) return;
                await ReloadEditorOptionsAsync();
                RebuildSeedSuggestions();
                ClearFieldSeedView();
                UpdateCollectionViewSource(receipt.SeedId);
            }, error =>
            {
                _userDialog.Error(error);
                return Task.CompletedTask;
            });
        }
        finally { _ignoreDiscard = false; SetSaving(false); }
    }

    public ICommand DeleteSeedCommand => field
        ??= new LambdaCommandAsync(OnDeleteSeedCommandExecuted, () => SelectedItem != null && !_isSaving);

    private async Task OnDeleteSeedCommandExecuted()
    {
        if (_isSaving || SelectedItem is not { } selected) return;
        var reason = _userDialog.RequestDeleteReason(selected.Plant.PlantSort.Name,
            selected.SeedsInfo.DisposeComment.ToNullable());
        if (reason is null) return;
        SetSaving(true);
        try
        {
            var result = await _seedsService.DeleteSeedWithReasonAsync(selected.Id, reason);
            result.Match(receipt =>
            {
                _ignoreDiscard = true;
                SelectedItem = null;
                if (Seeds is { } inventory)
                {
                    foreach (var cached in inventory.Where(seed => seed.Id == receipt.SeedId).ToArray())
                        inventory.Remove(cached);
                    RebuildSeedSuggestions();
                    UpdateCollectionViewSource();
                }
                return true;
            }, error =>
            {
                _userDialog.Error(error);
                return false;
            });
        }
        finally { _ignoreDiscard = false; SetSaving(false); }
    }

    public ICommand UpdateSeedsInfoCommand => field
        ??= new LambdaCommandAsync(OnUpdateSeedsInfoCommandExecuted,
            () => SelectedItem != null && !_isSaving && !EditorHasBindingErrors
                && SeedSortDetail is { HasErrors: false } && EditedProducerId > 0
                && EditedItem is { Package.HasErrors: false });

    private async Task OnUpdateSeedsInfoCommandExecuted()
    {
        if (SeedSortDetail is not { } detail || EditedItem is not { } draft) return;
        if (!_userDialog.YesNoQuestion(
                $"Сохранить изменения упаковки? Характеристики сорта «{draft.SortName}» и выбранный производитель будут изменены у всех записей этого сорта.",
                "Редактирование семян")) return;
        var id = draft.Id;
        SetSaving(true);
        try
        {
            var result = await SeedEditMapper.ToRequest(draft, detail, EditedProducerId).Match(
                request => _seedsService.SaveSeedEditAsync(request),
                error => Task.FromResult(Result.Failure<SeedEditResultFromViewModel>(error)));
            await result.Match(async _ =>
            {
                _ignoreDiscard = true;
                if ((await RefreshSeedsAsync()).IsFailure) return;
                await ReloadEditorOptionsAsync();
                SelectedSeedsViewItem = SeedsView!.Cast<SeedsFromViewModel>().FirstOrDefault(s => s.Id == id);
                RebuildSeedSuggestions();
            }, error =>
            {
                _userDialog.Error(error);
                return Task.CompletedTask;
            });
        }
        finally { _ignoreDiscard = false; SetSaving(false); }
    }

    public ICommand CancelUpdateSeedsInfoCommand => field
        ??= new LambdaCommand(() => { TryDiscardSeedChanges(); });

    private void SetSaving(bool value)
    {
        _isSaving = value;
        OnPropertyChanged(nameof(CanEditSeedDetails));
        CommandManager.InvalidateRequerySuggested();
    }

    public ICommand CreateSeedsReportCommand => field
        ??= new LambdaCommand(CreateSeedReport, () => Seeds != null && IsActive);

    public ICommand CreateFilteredSeedsReportCommand => field
        ??= new LambdaCommand(CreateFilteredSeedReport, () => SeedsView != null && IsActive);
}
