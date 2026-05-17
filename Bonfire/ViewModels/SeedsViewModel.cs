using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Bonfire.Data;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models;
using Bonfire.Services.Extensions;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bonfire.ViewModels;

public class SeedsViewModel : ViewModel
{
    private readonly ISeedsService _SeedsService;
    private readonly IUserDialog _UserDialog;
    private readonly IReportService _ReportService;

    public SeedsViewModel(ISeedsService seedsService, IUserDialog userDialog, IReportService reportService)
    {
        _SeedsService = seedsService;
        _UserDialog = userDialog;
        _ReportService = reportService;
        _SeedsView = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(SeedsFromViewModel.Culture), ListSortDirection.Ascending),
                new SortDescription(nameof(SeedsFromViewModel.Sort), ListSortDirection.Ascending),
                new SortDescription(nameof(SeedsFromViewModel.Producer), ListSortDirection.Ascending)
            }
        };
        _SeedsView.Filter += _SeedsViewSource_Filter;

        _CultureListView = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(CultureFromViewModel.Name), ListSortDirection.Ascending)
            }
        };
        _CultureListView.Filter += _CultureListView_Filter;

        _SortListView = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(SortFromSeedsViewModel.Name), ListSortDirection.Ascending)
            }
        };
        _SortListView.Filter += _SortListView_Filter;

        _ProducerListView = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(ProducerFromViewModel.Name), ListSortDirection.Ascending)
            }
        };
        _ProducerListView.Filter += _ProducerListView_Filter;
    }

    private bool _IsActive;
    public bool IsActive
    {
        get => _IsActive;
        set
        {
            Set(ref _IsActive, value);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    // Фильтрация главного списка

    public ICollectionView? SeedsView => _SeedsView.View;
    private readonly CollectionViewSource _SeedsView;

    private string _SeedFilter = "-Выбрать все-";
    public string SeedFilter
    {
        get => _SeedFilter;
        set
        {
            if (Set(ref _SeedFilter, value))
            {
                SelectedItem = null;
                SeedsView?.Refresh();
            }
        }
    }

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

    private ObservableCollection<Seed> _Seeds;
    public ObservableCollection<Seed> Seeds
    {
        get => _Seeds;
        set => Set(ref _Seeds, value);
    }

    private SeedsFromViewModel _SelectedSeedsViewItem;
    public SeedsFromViewModel SelectedSeedsViewItem
    {
        get => _SelectedSeedsViewItem;
        set
        {
            Set(ref _SelectedSeedsViewItem, value);
            SelectedItem = value != null ? Seeds.First(s => s.Id == value.Id) : null;
        }
    }

    private Seed _SelectedItem;
    public Seed SelectedItem
    {
        get => _SelectedItem;
        set
        {
            if (Set(ref _SelectedItem, value))
                CopySeedToEditItem(SelectedItem, EditedItem);
        }
    }

    private Seed _EditedItem = new()
    {
        Plant = new Plant()
        {
            PlantCulture = new PlantCulture(),
            PlantSort = new PlantSort()
            {
                Producer = new Producer()
            }
        },
        SeedsInfo = new SeedsInfo()
    };
    public Seed EditedItem
    {
        get => _EditedItem;
        set => Set(ref _EditedItem, value);
    }

    private List<string> _ListCulture = ["-Выбрать все-"];
    public List<string> ListCulture
    {
        get => _ListCulture;
        set => Set(ref _ListCulture, value);
    }

    private bool _IsHaving = true;
    public bool IsHaving
    {
        get => _IsHaving;
        set
        {
            Set(ref _IsHaving, value);
            SelectedItem = null;
            SeedsView?.Refresh();
        }
    }

    private string _AddQuantityInPac = string.Empty;
    public string AddQuantityInPac
    {
        get => _AddQuantityInPac;
        set => Set(ref _AddQuantityInPac, value);
    }

    private string _AddQuantityPac = "1";
    public string AddQuantityPac
    {
        get => _AddQuantityPac;
        set => Set(ref _AddQuantityPac, value);
    }

    private DateTime _AddBestBy = DateTime.Parse($"31.12.{DateTime.Now.Year + 1}");
    public DateTime AddBestBy
    {
        get => _AddBestBy;
        set => Set(ref _AddBestBy, value);
    }

    private string _AddCostPack = "0";
    public string AddCostPack
    {
        get => _AddCostPack;
        set => Set(ref _AddCostPack, value);
    }

    // Источник семян (радио-кнопки)

    private string _SeedSource = string.Empty;
    internal string SeedSource
    {
        get => _SeedSource;
        set => SetSeedSource(value);
    }

    private void SetSeedSource(string value)
    {
        if (_SeedSource == value) return;
        _SeedSource = value;
        OnPropertyChanged(nameof(IsSold));
        OnPropertyChanged(nameof(IsDonated));
        OnPropertyChanged(nameof(IsCollected));
    }

    public bool IsSold
    {
        get => _SeedSource == "Куплено";
        set { if (value) SetSeedSource("Куплено"); }
    }

    public bool IsDonated
    {
        get => _SeedSource == "Подарено";
        set { if (value) SetSeedSource("Подарено"); }
    }

    public bool IsCollected
    {
        get => _SeedSource == "Собрано";
        set { if (value) { SetSeedSource("Собрано"); AddProducer = "Свои семена"; } }
    }

    // Примечание

    private string _AddNote;
    public string AddNote
    {
        get => _AddNote;
        set => Set(ref _AddNote, value);
    }

    // Единицы измерения

    private List<string> _AddSizeList = new() { "Граммы", "Штуки" };
    public List<string> AddSizeList
    {
        get => _AddSizeList;
        set => Set(ref _AddSizeList, value);
    }

    private string _AddSize;
    public string AddSize
    {
        get => _AddSize;
        set => Set(ref _AddSize, value);
    }

    // Класс растения

    private ObservableCollection<string> _AddClassList = new(PlantClassList.GetClassList());
    public ObservableCollection<string> AddClassList
    {
        get => _AddClassList;
        set => Set(ref _AddClassList, value);
    }

    private string _AddClass;
    public string AddClass
    {
        get => _AddClass;
        set => Set(ref _AddClass, value);
    }

    // Выбор культуры

    public ICollectionView? CultureListView => _CultureListView.View;
    private readonly CollectionViewSource _CultureListView;

    private void _CultureListView_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not CultureFromViewModel culture || string.IsNullOrEmpty(AddCulture)) return;
        if (culture.Name != null && !culture.Name.Contains(AddCulture, StringComparison.OrdinalIgnoreCase))
            e.Accepted = false;
    }

    private ObservableCollection<CultureFromViewModel> _AddCultureList = new();
    public ObservableCollection<CultureFromViewModel> AddCultureList
    {
        get => _AddCultureList;
        set => Set(ref _AddCultureList, value);
    }

    private string _AddCulture;
    public string AddCulture
    {
        get => _AddCulture;
        set
        {
            if (Set(ref _AddCulture, value))
                CultureListView?.Refresh();
        }
    }

    // Выбор сорта

    public ICollectionView SortListView => _SortListView.View;
    private readonly CollectionViewSource _SortListView;

    private void _SortListView_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not SortFromSeedsViewModel sort || string.IsNullOrEmpty(AddSort)) return;
        if (sort.Name != null && !sort.Name.Contains(AddSort, StringComparison.OrdinalIgnoreCase))
            e.Accepted = false;
    }

    private ObservableCollection<SortFromSeedsViewModel> _AddSortList = new();
    public ObservableCollection<SortFromSeedsViewModel> AddSortList
    {
        get => _AddSortList;
        set => Set(ref _AddSortList, value);
    }

    private string _AddSort;
    public string AddSort
    {
        get => _AddSort;
        set
        {
            if (Set(ref _AddSort, value))
                SortListView?.Refresh();
        }
    }

    // Выбор производителя

    public ICollectionView ProducerListView => _ProducerListView.View;
    private readonly CollectionViewSource _ProducerListView;

    private void _ProducerListView_Filter(object sender, FilterEventArgs e)
    {
        if (!(e.Item is ProducerFromViewModel producer) || string.IsNullOrEmpty(AddProducer)) return;
        if (producer.Name != null && !producer.Name.Contains(AddProducer, StringComparison.OrdinalIgnoreCase))
            e.Accepted = false;
    }

    private ObservableCollection<ProducerFromViewModel> _AddProducerList = new();
    public ObservableCollection<ProducerFromViewModel> AddProducerList
    {
        get => _AddProducerList;
        set => Set(ref _AddProducerList, value);
    }

    private string _AddProducer;
    public string AddProducer
    {
        get => _AddProducer;
        set
        {
            if (Set(ref _AddProducer, value))
            {
                ProducerListView?.Refresh();
                if (_AddProducer == "Свои семена")
                    IsCollected = true;
            }
        }
    }

    // Методы

    private async Task LoadSeed()
    {
        _SeedsView.Source = _SeedsService.Seeds.AsEnumerable()
            .Select(CreateSeedsFromViewModel).SortSeeds();
        Seeds = new ObservableCollection<Seed>(await _SeedsService.Seeds.ToArrayAsync());
        OnPropertyChanged(nameof(SeedsView));
    }

    private void LoadListCulture()
    {
        var listCultureQuery = _SeedsService.Seeds
            .Select(seeds => seeds.Plant.PlantCulture.Name)
            .Distinct()
            .OrderBy(s => s);
        var addListCulture = _SeedsService.Seeds
            .Select(seeds => new CultureFromViewModel
            {
                Id = seeds.Plant.PlantCulture.Id,
                Name = seeds.Plant.PlantCulture.Name
            }).AsEnumerable()
            .Distinct(s => s.Name)
            .OrderBy(s => s.Name);
        ListCulture.AddRange(listCultureQuery.ToListAsync().Result);
        AddCultureList.AddRange(addListCulture.ToList());
        _CultureListView.Source = AddCultureList;
        OnPropertyChanged(nameof(CultureListView));
    }

    private void LoadListSort()
    {
        var addListSort = _SeedsService.Seeds
            .Select(seeds => new SortFromSeedsViewModel
            {
                Id = seeds.Plant.PlantSort.Id,
                Name = seeds.Plant.PlantSort.Name
            }).AsEnumerable()
            .Distinct(s => s.Name)
            .OrderBy(s => s.Name);
        AddSortList.AddRange(addListSort.ToList());
        _SortListView.Source = AddSortList;
        OnPropertyChanged(nameof(SortListView));
    }

    private void LoadListProducer()
    {
        var addListProducer = _SeedsService.Seeds
            .Select(seeds => new ProducerFromViewModel
            {
                Id = seeds.Plant.PlantSort.Producer.Id,
                Name = seeds.Plant.PlantSort.Producer.Name
            }).AsEnumerable()
            .Distinct(s => s.Name)
            .OrderBy(s => s.Name);
        AddProducerList.AddRange(addListProducer.ToList());
        _ProducerListView.Source = AddProducerList;
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
        AddBestBy = DateTime.Parse($"31.12.{DateTime.Now.Year + 1}");
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

    private void UpdateCollectionViewSource(int id = -1)
    {
        var newCollection = Seeds.Select(CreateSeedsFromViewModel).SortSeeds();
        var collection = newCollection.ToArray();
        _SeedsView.Source = collection;
        if (id != -1)
        {
            var current = collection.FirstOrDefault(s => s.Id == id);
            _SeedsView.View.MoveCurrentTo(current);
        }
        OnPropertyChanged(nameof(SeedsView));
        OnPropertyChanged(nameof(ListCulture));
    }

    private SeedsFromViewModel CreateSeedsFromViewModel(Seed seed) => new()
    {
        Id = seed.Id,
        Culture = seed.Plant.PlantCulture.Name,
        Sort = seed.Plant.PlantSort.Name,
        Producer = seed.Plant.PlantSort.Producer.Name,
        ExpirationDate = seed.SeedsInfo.ExpirationDate,
        QuantityPack = seed.SeedsInfo.QuantityPack,
        WeightPack = seed.SeedsInfo.WeightPack,
        AmountSeedsQuantity = seed.SeedsInfo.AmountSeeds,
        AmountSeedsWeight = seed.SeedsInfo.AmountSeedsWeight
    };

    private void CopySeedToEditItem(Seed seedFrom, Seed seedTo)
    {
        if (seedFrom == null) return;

        seedTo.Id = seedFrom.Id;
        seedTo.SeedsInfoId = seedFrom.SeedsInfoId;

        seedTo.Plant.Id = seedFrom.Plant.Id;
        seedTo.Plant.PlantCulture.Id = seedFrom.Plant.PlantCulture.Id;
        seedTo.Plant.PlantCulture.Name = seedFrom.Plant.PlantCulture.Name;
        seedTo.Plant.PlantCulture.Class = seedFrom.Plant.PlantCulture.Class;
        seedTo.Plant.PlantSort.Id = seedFrom.Plant.PlantSort.Id;
        seedTo.Plant.PlantSort.Name = seedFrom.Plant.PlantSort.Name;
        seedTo.Plant.PlantSort.Description = seedFrom.Plant.PlantSort.Description;
        seedTo.Plant.PlantSort.MinGerminationTime = seedFrom.Plant.PlantSort.MinGerminationTime;
        seedTo.Plant.PlantSort.MaxGerminationTime = seedFrom.Plant.PlantSort.MaxGerminationTime;
        seedTo.Plant.PlantSort.AgeOfSeedlings = seedFrom.Plant.PlantSort.AgeOfSeedlings;
        seedTo.Plant.PlantSort.GrowingSeason = seedFrom.Plant.PlantSort.GrowingSeason;
        seedTo.Plant.PlantSort.LandingPattern = seedFrom.Plant.PlantSort.LandingPattern;
        seedTo.Plant.PlantSort.PlantHeight = seedFrom.Plant.PlantSort.PlantHeight;
        seedTo.Plant.PlantSort.PlantColor = seedFrom.Plant.PlantSort.PlantColor;
        seedTo.Plant.PlantSort.Producer.Id = seedFrom.Plant.PlantSort.Producer.Id;
        seedTo.Plant.PlantSort.Producer.Name = seedFrom.Plant.PlantSort.Producer.Name;

        seedTo.SeedsInfo.Id = seedFrom.SeedsInfo.Id;
        seedTo.SeedsInfo.WeightPack = seedFrom.SeedsInfo.WeightPack;
        seedTo.SeedsInfo.QuantityPack = seedFrom.SeedsInfo.QuantityPack;
        seedTo.SeedsInfo.PurchaseDate = seedFrom.SeedsInfo.PurchaseDate;
        seedTo.SeedsInfo.ExpirationDate = seedFrom.SeedsInfo.ExpirationDate;
        seedTo.SeedsInfo.CostPack = seedFrom.SeedsInfo.CostPack;
        seedTo.SeedsInfo.DisposeComment = seedFrom.SeedsInfo.DisposeComment;
        seedTo.SeedsInfo.AmountSeeds = seedFrom.SeedsInfo.AmountSeeds;
        seedTo.SeedsInfo.AmountSeedsWeight = seedFrom.SeedsInfo.AmountSeedsWeight;
        seedTo.SeedsInfo.SeedSource = seedFrom.SeedsInfo.SeedSource;
        seedTo.SeedsInfo.Note = seedFrom.SeedsInfo.Note;

        OnPropertyChanged(nameof(EditedItem));
        OnPropertyChanged(nameof(SelectedItem));
    }

    private void UpdateCollectionSeedsViewModel(Seed newSeed)
    {
        Seeds.Add(newSeed);

        if (!AddCultureList.Contains(c => c.Name == newSeed.Plant.PlantCulture.Name))
        {
            AddCultureList.Add(new CultureFromViewModel
            {
                Id = newSeed.Plant.PlantCulture.Id,
                Name = newSeed.Plant.PlantCulture.Name
            });
            ListCulture.Add(newSeed.Plant.PlantCulture.Name);
            ListCulture = new List<string>(ListCulture.OrderBy(c => c));
            AddCultureList = new ObservableCollection<CultureFromViewModel>(AddCultureList.OrderBy(c => c.Name));
        }

        if (!AddProducerList.Contains(p => p.Name == newSeed.Plant.PlantSort.Producer.Name))
        {
            AddProducerList.Add(new ProducerFromViewModel
            {
                Id = newSeed.Plant.PlantSort.Producer.Id,
                Name = newSeed.Plant.PlantSort.Producer.Name
            });
            AddProducerList = new ObservableCollection<ProducerFromViewModel>(AddProducerList.OrderBy(c => c.Name));
        }

        if (AddSortList.Contains(s => s.Name == newSeed.Plant.PlantSort.Name)) return;
        AddSortList.Add(new SortFromSeedsViewModel
        {
            Id = newSeed.Plant.PlantSort.Id,
            Name = newSeed.Plant.PlantSort.Name
        });
        AddSortList = new ObservableCollection<SortFromSeedsViewModel>(AddSortList.OrderBy(c => c.Name));
    }

    private void RemoveOfAddLists(Seed deleteSeed)
    {
        if (!Seeds.Contains(s => s.Plant.PlantCulture.Name == deleteSeed.Plant.PlantCulture.Name))
            AddCultureList.Remove(AddCultureList.Find(c => c.Name == deleteSeed.Plant.PlantCulture.Name)!);

        if (!Seeds.Contains(s => s.Plant.PlantSort.Name == deleteSeed.Plant.PlantSort.Name))
            AddSortList.Remove(AddSortList.Find(c => c.Name == deleteSeed.Plant.PlantSort.Name)!);

        if (!Seeds.Contains(s => s.Plant.PlantSort.Producer.Name == deleteSeed.Plant.PlantSort.Producer.Name))
            AddProducerList.Remove(AddProducerList.Find(c => c.Name == deleteSeed.Plant.PlantSort.Producer.Name)!);
    }

    private void CreateSeedReport()
    {
        var items = Seeds.Select(s => new SeedReportItem(
            s.Plant.PlantCulture.Name,
            s.Plant.PlantSort.Name,
            s.Plant.PlantSort.Producer.Name,
            s.SeedsInfo.ExpirationDate.ToShortDateString(),
            s.SeedsInfo.WeightPack,
            s.SeedsInfo.QuantityPack))
            .OrderBy(s => s.Culture);
        _ReportService.CreateSeedsReport(items);
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
        _ReportService.CreateSeedsReport(items, "Семена_выборка");
    }

    // Команды

    private ICommand _LoadDataCommand;
    public ICommand LoadDataCommand => _LoadDataCommand
        ??= new LambdaCommandAsync(OnLoadDataCommandExecuted);

    private async Task OnLoadDataCommandExecuted()
    {
        if (Seeds != null)
        {
            UpdateCollectionViewSource();
            return;
        }
        await LoadSeed();
        LoadListCulture();
        LoadListSort();
        LoadListProducer();
    }

    private ICommand _SeedsChoiceClassCommand;
    public ICommand SeedsChoiceClassCommand => _SeedsChoiceClassCommand
        ??= new LambdaCommandAsync(OnSeedsChoiceClassCommandExecuted);

    private async Task OnSeedsChoiceClassCommandExecuted(object p)
    {
        var filteredSeeds = p != null && p.ToString() != "Выбрать все"
            ? Seeds.Where(seeds => seeds.Plant.PlantCulture.Class == p.ToString())
            : Seeds;

        _SeedsView.Source = filteredSeeds.Select(CreateSeedsFromViewModel).SortSeeds().ToArray();
        OnPropertyChanged(nameof(SeedsView));
    }

    private ICommand _AddOrCorrectSeedCommand;
    public ICommand AddOrCorrectSeedCommand => _AddOrCorrectSeedCommand
        ??= new LambdaCommandAsync(OnAddOrCorrectSeedCommandExecuted, Verification);

    private async Task OnAddOrCorrectSeedCommandExecuted()
    {
        var request = new AddSeedRequest(
            AddCulture, AddSort, AddProducer, AddClass,
            SeedSource, AddSize,
            AddQuantityInPac.DoubleParseAdvanced(),
            AddQuantityPac.DoubleParseAdvanced(),
            AddCostPack.DecimalParseAdvanced(),
            AddBestBy, AddNote);

        var (seed, isNew) = await _SeedsService.AddOrUpdateSeedAsync(request, Seeds).ConfigureAwait(false);

        if (isNew)
            UpdateCollectionSeedsViewModel(seed);

        ClearFieldSeedView();
        UpdateCollectionViewSource(seed.Id);
    }

    private ICommand _DeleteSeedCommand;
    public ICommand DeleteSeedCommand => _DeleteSeedCommand
        ??= new LambdaCommandAsync(OnDeleteSeedCommandExecuted, () => SelectedItem != null);

    private async Task OnDeleteSeedCommandExecuted()
    {
        if (!_UserDialog.YesNoQuestion(
                $"Вы уверены, что хотите удалить семена сорта - {SelectedItem.Plant.PlantSort.Name}",
                "Удаление семян")) return;

        var deleteSeed = await _SeedsService.DeleteSeed(SelectedItem).ConfigureAwait(false);
        Seeds.Remove(deleteSeed);
        RemoveOfAddLists(deleteSeed);
        UpdateCollectionViewSource();
    }

    private ICommand _UpdateSeedsInfoCommand;
    public ICommand UpdateSeedsInfoCommand => _UpdateSeedsInfoCommand
        ??= new LambdaCommandAsync(OnUpdateSeedsInfoCommandExecuted, () => EditedItem != null);

    private async Task OnUpdateSeedsInfoCommandExecuted()
    {
        if (!_UserDialog.YesNoQuestion(
                $"Вы уверены, что хотите изменить информацию о семенах сорта - {EditedItem.Plant.PlantSort.Name}",
                "Редактирование семян")) return;

        CopySeedToEditItem(EditedItem, SelectedItem);
        if (SelectedItem.SeedsInfo.AmountSeeds > 0)
            SelectedItem.SeedsInfo.AmountSeedsWeight = 0;
        await _SeedsService.UpdateSeed(SelectedItem).ConfigureAwait(false);
        UpdateCollectionViewSource(SelectedItem.Id);
    }

    private ICommand _CancelUpdateSeedsInfoCommand;
    public ICommand CancelUpdateSeedsInfoCommand => _CancelUpdateSeedsInfoCommand
        ??= new LambdaCommandAsync(async () => UpdateCollectionViewSource(SelectedItem.Id));

    private ICommand _CreateSeedsReportCommand;
    public ICommand CreateSeedsReportCommand => _CreateSeedsReportCommand
        ??= new LambdaCommand(CreateSeedReport, () => Seeds != null && IsActive);

    private ICommand _CreateFilteredSeedsReportCommand;
    public ICommand CreateFilteredSeedsReportCommand => _CreateFilteredSeedsReportCommand
        ??= new LambdaCommand(CreateFilteredSeedReport, () => SeedsView != null && IsActive);
}
