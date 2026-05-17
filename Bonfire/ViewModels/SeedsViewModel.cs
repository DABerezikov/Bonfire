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
    private readonly ISeedsService _seedsService;
    private readonly IUserDialog _userDialog;
    private readonly IReportService _reportService;

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
            Set(ref field, value);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    // Фильтрация главного списка

    public ICollectionView? SeedsView => _seedsView.View;
    private readonly CollectionViewSource _seedsView;

    public string SeedFilter
    {
        get;
        set
        {
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

    public ObservableCollection<Seed> Seeds
    {
        get;
        set => Set(ref field, value);
    }

    public SeedsFromViewModel SelectedSeedsViewItem
    {
        get;
        set
        {
            Set(ref field, value);
            SelectedItem = value != null ? Seeds.First(s => s.Id == value.Id) : null;
        }
    }

    public Seed SelectedItem
    {
        get;
        set
        {
            if (Set(ref field, value))
                CopySeedToEditItem(SelectedItem, EditedItem);
        }
    }

    public Seed EditedItem
    {
        get;
        set => Set(ref field, value);
    } = new()
    {
        Plant = new Plant
        {
            PlantCulture = new PlantCulture(),
            PlantSort = new PlantSort
            {
                Producer = new Producer()
            }
        },
        SeedsInfo = new SeedsInfo()
    };

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
    } = DateTime.Parse($"31.12.{DateTime.Now.Year + 1}");

    public string AddCostPack
    {
        get;
        set => Set(ref field, value);
    } = "0";

    // Источник семян (радио-кнопки)

    private string _seedSource = string.Empty;
    internal string SeedSource
    {
        get => _seedSource;
        set => SetSeedSource(value);
    }

    private void SetSeedSource(string value)
    {
        if (_seedSource == value) return;
        _seedSource = value;
        OnPropertyChanged(nameof(IsSold));
        OnPropertyChanged(nameof(IsDonated));
        OnPropertyChanged(nameof(IsCollected));
    }

    public bool IsSold
    {
        get => _seedSource == "Куплено";
        set { if (value) SetSeedSource("Куплено"); }
    }

    public bool IsDonated
    {
        get => _seedSource == "Подарено";
        set { if (value) SetSeedSource("Подарено"); }
    }

    public bool IsCollected
    {
        get => _seedSource == "Собрано";
        set { if (value) { SetSeedSource("Собрано"); AddProducer = "Свои семена"; } }
    }

    // Примечание

    public string AddNote
    {
        get;
        set => Set(ref field, value);
    }

    // Единицы измерения

    public List<string> AddSizeList
    {
        get;
        set => Set(ref field, value);
    } = new() { "Граммы", "Штуки" };

    public string AddSize
    {
        get;
        set => Set(ref field, value);
    }

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
    }

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
    } = new();

    public string AddCulture
    {
        get;
        set
        {
            if (Set(ref field, value))
                CultureListView?.Refresh();
        }
    }

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
    } = new();

    public string AddSort
    {
        get;
        set
        {
            if (Set(ref field, value))
                SortListView?.Refresh();
        }
    }

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
    } = new();

    public string AddProducer
    {
        get;
        set
        {
            if (Set(ref field, value))
            {
                ProducerListView?.Refresh();
                if (field == "Свои семена")
                    IsCollected = true;
            }
        }
    }

    // Методы

    private async Task LoadSeed()
    {
        _seedsView.Source = _seedsService.Seeds.AsEnumerable()
            .Select(CreateSeedsFromViewModel).SortSeeds();
        Seeds = new ObservableCollection<Seed>(await _seedsService.Seeds.ToArrayAsync());
        OnPropertyChanged(nameof(SeedsView));
    }

    private void LoadListCulture()
    {
        var listCultureQuery = _seedsService.Seeds
            .Select(seeds => seeds.Plant.PlantCulture.Name)
            .Distinct()
            .OrderBy(s => s);
        var addListCulture = _seedsService.Seeds
            .Select(seeds => new CultureFromViewModel
            {
                Id = seeds.Plant.PlantCulture.Id,
                Name = seeds.Plant.PlantCulture.Name
            }).AsEnumerable()
            .Distinct(s => s.Name)
            .OrderBy(s => s.Name);
        ListCulture.AddRange(listCultureQuery.ToListAsync().Result);
        AddCultureList.AddRange(addListCulture.ToList());
        _cultureListView.Source = AddCultureList;
        OnPropertyChanged(nameof(CultureListView));
    }

    private void LoadListSort()
    {
        var addListSort = _seedsService.Seeds
            .Select(seeds => new SortFromSeedsViewModel
            {
                Id = seeds.Plant.PlantSort.Id,
                Name = seeds.Plant.PlantSort.Name
            }).AsEnumerable()
            .Distinct(s => s.Name)
            .OrderBy(s => s.Name);
        AddSortList.AddRange(addListSort.ToList());
        _sortListView.Source = AddSortList;
        OnPropertyChanged(nameof(SortListView));
    }

    private void LoadListProducer()
    {
        var addListProducer = _seedsService.Seeds
            .Select(seeds => new ProducerFromViewModel
            {
                Id = seeds.Plant.PlantSort.Producer.Id,
                Name = seeds.Plant.PlantSort.Producer.Name
            }).AsEnumerable()
            .Distinct(s => s.Name)
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
        _seedsView.Source = collection;
        if (id != -1)
        {
            var current = collection.FirstOrDefault(s => s.Id == id);
            _seedsView.View.MoveCurrentTo(current);
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

    // Команды

    public ICommand LoadDataCommand => field
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

    public ICommand SeedsChoiceClassCommand => field
        ??= new LambdaCommandAsync(OnSeedsChoiceClassCommandExecuted);

    private async Task OnSeedsChoiceClassCommandExecuted(object p)
    {
        var filteredSeeds = p != null && p.ToString() != "Выбрать все"
            ? Seeds.Where(seeds => seeds.Plant.PlantCulture.Class == p.ToString())
            : Seeds;

        _seedsView.Source = filteredSeeds.Select(CreateSeedsFromViewModel).SortSeeds().ToArray();
        OnPropertyChanged(nameof(SeedsView));
    }

    public ICommand AddOrCorrectSeedCommand => field
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

        var (seed, isNew) = await _seedsService.AddOrUpdateSeedAsync(request, Seeds).ConfigureAwait(false);

        if (isNew)
            UpdateCollectionSeedsViewModel(seed);

        ClearFieldSeedView();
        UpdateCollectionViewSource(seed.Id);
    }

    public ICommand DeleteSeedCommand => field
        ??= new LambdaCommandAsync(OnDeleteSeedCommandExecuted, () => SelectedItem != null);

    private async Task OnDeleteSeedCommandExecuted()
    {
        if (!_userDialog.YesNoQuestion(
                $"Вы уверены, что хотите удалить семена сорта - {SelectedItem.Plant.PlantSort.Name}",
                "Удаление семян")) return;

        var deleteSeed = await _seedsService.DeleteSeed(SelectedItem).ConfigureAwait(false);
        Seeds.Remove(deleteSeed);
        RemoveOfAddLists(deleteSeed);
        UpdateCollectionViewSource();
    }

    public ICommand UpdateSeedsInfoCommand => field
        ??= new LambdaCommandAsync(OnUpdateSeedsInfoCommandExecuted, () => EditedItem != null);

    private async Task OnUpdateSeedsInfoCommandExecuted()
    {
        if (!_userDialog.YesNoQuestion(
                $"Вы уверены, что хотите изменить информацию о семенах сорта - {EditedItem.Plant.PlantSort.Name}",
                "Редактирование семян")) return;

        CopySeedToEditItem(EditedItem, SelectedItem);
        if (SelectedItem.SeedsInfo.AmountSeeds > 0)
            SelectedItem.SeedsInfo.AmountSeedsWeight = 0;
        await _seedsService.UpdateSeed(SelectedItem).ConfigureAwait(false);
        UpdateCollectionViewSource(SelectedItem.Id);
    }

    public ICommand CancelUpdateSeedsInfoCommand => field
        ??= new LambdaCommandAsync(async () => UpdateCollectionViewSource(SelectedItem.Id));

    public ICommand CreateSeedsReportCommand => field
        ??= new LambdaCommand(CreateSeedReport, () => Seeds != null && IsActive);

    public ICommand CreateFilteredSeedsReportCommand => field
        ??= new LambdaCommand(CreateFilteredSeedReport, () => SeedsView != null && IsActive);
}
