using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models;
using Bonfire.Services.Extensions;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bonfire.ViewModels;

public class SeedlingsViewModel : ViewModel
{
    private readonly ISeedlingsService _SeedlingsService;
    private readonly ISeedsService _SeedsService;
    private readonly IUserDialog _UserDialog;
    private readonly IReportService _ReportService;

    public SeedlingsViewModel(ISeedlingsService seedlings, ISeedsService seedsService, IUserDialog dialog, IReportService reportService)
    {
        _SeedlingsService = seedlings;
        _SeedsService = seedsService;
        _UserDialog = dialog;
        _ReportService = reportService;

        _SeedlingsView = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(SeedlingFromViewModel.LandingData), ListSortDirection.Ascending),
                new SortDescription(nameof(SeedlingFromViewModel.Culture), ListSortDirection.Ascending),
                new SortDescription(nameof(SeedlingFromViewModel.Sort), ListSortDirection.Ascending),
                new SortDescription(nameof(SeedlingFromViewModel.Producer), ListSortDirection.Ascending)
            }
        };
        _SeedlingsView.Filter += SeedsViewSource_Filter;

        _PlantListView = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(PlantFromViewModel.Culture), ListSortDirection.Ascending),
                new SortDescription(nameof(PlantFromViewModel.Sort), ListSortDirection.Ascending),
                new SortDescription(nameof(PlantFromViewModel.Producer), ListSortDirection.Ascending)
            }
        };
        _PlantListView.Filter += PlantListView_Filter;

        _CultureListView = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(CultureFromViewModel.Name), ListSortDirection.Ascending)
            }
        };
        _CultureListView.Filter += CultureListView_Filter;

        _SortListView = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(SortFromSeedlingsViewModel.Sort), ListSortDirection.Ascending)
            }
        };
        _SortListView.Filter += SortListView_Filter;
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

    public ICollectionView? SeedlingsView => _SeedlingsView.View;
    private readonly CollectionViewSource _SeedlingsView;

    private string _SeedlingFilter = "-Выбрать все-";
    public string SeedlingFilter
    {
        get => _SeedlingFilter;
        set
        {
            if (Set(ref _SeedlingFilter, value))
            {
                SelectedItem = null;
                SeedlingsView?.Refresh();
            }
        }
    }

    private void SeedsViewSource_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not SeedlingFromViewModel seedling) return;
        if (SeedlingFilter != "-Выбрать все-")
        {
            if (IsHaving)
            {
                if (seedling.Culture != null && (!seedling.Culture.Contains(SeedlingFilter, StringComparison.OrdinalIgnoreCase) ||
                                                 seedling.IsDead == true))
                    e.Accepted = false;
            }
            else
            {
                if (seedling.Culture != null && !seedling.Culture.Contains(SeedlingFilter, StringComparison.OrdinalIgnoreCase))
                    e.Accepted = false;
            }
        }
        else
        {
            if (!IsHaving) return;
            if (seedling.IsDead == true)
                e.Accepted = false;
        }
    }

    // Основные данные

    private ObservableCollection<Seedling> _Seedlings;
    public ObservableCollection<Seedling> Seedlings
    {
        get => _Seedlings;
        set => Set(ref _Seedlings, value);
    }

    private SeedlingFromViewModel _SelectedSeedlingViewItem;
    public SeedlingFromViewModel SelectedSeedlingViewItem
    {
        get => _SelectedSeedlingViewItem;
        set
        {
            Set(ref _SelectedSeedlingViewItem, value);
            SelectedItem = value is null ? null : Seedlings.First(s => s.Id == value.Id);
        }
    }

    private Seedling _SelectedItem;
    public Seedling SelectedItem
    {
        get => _SelectedItem;
        set
        {
            if (Set(ref _SelectedItem, value))
                CopySeedlingToEditItem(SelectedItem, EditedItem);
        }
    }

    private Seedling _EditedItem = new()
    {
        Plant = new Plant()
        {
            PlantCulture = new PlantCulture(),
            PlantSort = new PlantSort()
            {
                Producer = new Producer()
            }
        },
        SeedlingInfos =
        [
            new SeedlingInfo
            {
                Replants = [],
                Treatments = []
            }
        ]
    };
    public Seedling EditedItem
    {
        get => _EditedItem;
        set => Set(ref _EditedItem, value);
    }

    private List<string> _ListCulture = new() { "-Выбрать все-" };
    public List<string> ListCulture
    {
        get => _ListCulture;
        set => Set(ref _ListCulture, value);
    }

    // Источник рассады (радио-кнопки)

    private string _SeedlingSource = string.Empty;
    internal string SeedlingSource
    {
        get => _SeedlingSource;
        set => SetSeedlingSource(value);
    }

    private void SetSeedlingSource(string value)
    {
        if (_SeedlingSource == value) return;
        _SeedlingSource = value;
        OnPropertyChanged(nameof(IsSold));
        OnPropertyChanged(nameof(IsDonated));
        OnPropertyChanged(nameof(IsSeeds));
    }

    public bool IsSold
    {
        get => _SeedlingSource == "Куплено";
        set { if (value) SetSeedlingSource("Куплено"); }
    }

    public bool IsDonated
    {
        get => _SeedlingSource == "Подарено";
        set { if (value) SetSeedlingSource("Подарено"); }
    }

    public bool IsSeeds
    {
        get => _SeedlingSource == "Из семян";
        set { if (value) SetSeedlingSource("Из семян"); }
    }

    // Единица измерения

    private string _AddSize;
    public string AddSize
    {
        get => _AddSize;
        set => Set(ref _AddSize, value);
    }

    // Выбор растения

    public ICollectionView? PlantListView => _PlantListView.View;
    private readonly CollectionViewSource _PlantListView;

    private void PlantListView_Filter(object sender, FilterEventArgs e)
    {
        if (!(e.Item is PlantFromViewModel plant) || (string.IsNullOrEmpty(AddCulture) && string.IsNullOrEmpty(AddSort))) return;
        if (!string.IsNullOrEmpty(AddSort))
        {
            if (string.IsNullOrEmpty(AddCulture))
            {
                if (!plant.Sort!.Equals(AddSort, StringComparison.OrdinalIgnoreCase))
                    e.Accepted = false;
            }
            else
            {
                if (!(plant.Culture!.Equals(AddCulture, StringComparison.OrdinalIgnoreCase) &&
                      plant.Sort!.Equals(AddSort, StringComparison.OrdinalIgnoreCase)))
                    e.Accepted = false;
            }
        }
        else
        {
            if (!plant.Culture!.Equals(AddCulture, StringComparison.OrdinalIgnoreCase))
                e.Accepted = false;
        }
    }

    private ObservableCollection<PlantFromViewModel> _AddPlantList = new();
    public ObservableCollection<PlantFromViewModel> AddPlantList
    {
        get => _AddPlantList;
        set => Set(ref _AddPlantList, value);
    }

    private string _AddProducer;
    public string AddProducer
    {
        get => _AddProducer;
        set
        {
            if (!Set(ref _AddProducer, value)) return;
            if (!string.IsNullOrWhiteSpace(AddCulture) && !string.IsNullOrWhiteSpace(AddSort))
            {
                if (string.IsNullOrWhiteSpace(AddProducer)) return;
                CurrentPlant = AddPlantList.First(p => p.Producer + " " + p.ExpirationDate.Year == value
                                                       && p.Culture == AddCulture
                                                       && p.Sort == AddSort);
            }
            PlantListView?.Refresh();
        }
    }

    private PlantFromViewModel _CurrentPlant;
    public PlantFromViewModel CurrentPlant
    {
        get => _CurrentPlant;
        set
        {
            if (!Set(ref _CurrentPlant, value)) return;
            if (value is null) return;
            CurrentSeed = _SeedsService.Seeds.First(s => s.Id == CurrentPlant.Id);
        }
    }

    private Seed _CurrentSeed;
    public Seed CurrentSeed
    {
        get => _CurrentSeed;
        set
        {
            if (!Set(ref _CurrentSeed, value)) return;
            if (value is null) return;
            Plantable = CurrentSeed.SeedsInfo.AmountSeedsWeight > 0.0 ? CurrentSeed.SeedsInfo.AmountSeedsWeight : CurrentSeed.SeedsInfo.AmountSeeds;
            AddSize = CurrentSeed.SeedsInfo.AmountSeedsWeight > 0.0 ? "гр." : "шт.";
        }
    }

    private double? _Plantable;
    public double? Plantable
    {
        get => _Plantable;
        set => Set(ref _Plantable, value);
    }

    // Выбор культуры

    public ICollectionView? CultureListView => _CultureListView.View;
    private readonly CollectionViewSource _CultureListView;

    private void CultureListView_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not CultureFromViewModel culture || string.IsNullOrEmpty(AddCulture)) return;
        if (!culture.Name!.Contains(AddCulture, StringComparison.OrdinalIgnoreCase))
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
            if (!Set(ref _AddCulture, value)) return;
            AddSort = string.Empty;
            AddProducer = string.Empty;
            CurrentSeed = null;
            CurrentPlant = null;
            Plantable = null;
            AddSize = string.Empty;
            AddQuantity = 0.0;

            if (string.IsNullOrWhiteSpace(AddCulture)) return;
            var list = AddSortList.Select(p => p).Where(p => p.Culture == AddCulture).ToList();
            if (list.Count == 1)
                AddSort = AddSortList.First(p => p.Culture == AddCulture).Sort!;

            CultureListView?.Refresh();
            SortListView?.Refresh();
            PlantListView?.Refresh();
        }
    }

    // Выбор сорта

    public ICollectionView? SortListView => _SortListView.View;
    private readonly CollectionViewSource _SortListView;

    private void SortListView_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not SortFromSeedlingsViewModel sort ||
            (string.IsNullOrEmpty(AddSort) && string.IsNullOrEmpty(AddCulture))) return;
        if (string.IsNullOrEmpty(AddCulture)) return;
        if (!string.IsNullOrEmpty(AddSort))
        {
            if (!(sort.Culture!.Contains(AddCulture, StringComparison.OrdinalIgnoreCase) && sort.Sort!.Contains(AddSort, StringComparison.OrdinalIgnoreCase)))
                e.Accepted = false;
        }
        else
        {
            if (!sort.Culture!.Contains(AddCulture, StringComparison.OrdinalIgnoreCase))
                e.Accepted = false;
        }
    }

    private ObservableCollection<SortFromSeedlingsViewModel> _AddSortList = new();
    public ObservableCollection<SortFromSeedlingsViewModel> AddSortList
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
            if (!Set(ref _AddSort, value)) return;
            if (string.IsNullOrWhiteSpace(AddCulture) && string.IsNullOrWhiteSpace(AddSort)) return;
            var list = AddPlantList.Select(p => p).Where(p => p.Culture == AddCulture && p.Sort == AddSort).ToList();
            if (list.Count == 1)
                AddProducer = AddPlantList.First(p => p.Culture == AddCulture && p.Sort == AddSort).ToString();
            SortListView?.Refresh();
            PlantListView?.Refresh();
        }
    }

    // Количество и даты

    private string _AddQuantityString;
    public string AddQuantityString
    {
        get => _AddQuantityString;
        set
        {
            Set(ref _AddQuantityString, value);
            AddQuantity = AddQuantityString.DoubleParseAdvanced();
        }
    }

    private double _AddQuantity;
    public double AddQuantity
    {
        get => _AddQuantity;
        set
        {
            if (value > Plantable) value = (double)Plantable;
            Set(ref _AddQuantity, value);
        }
    }

    private DateTime _PlantingDate;
    public DateTime PlantingDate
    {
        get => _PlantingDate;
        set
        {
            if (!Set(ref _PlantingDate, value)) return;
            MoonPhase = _SeedlingsService.Lunar.GetMoonPhase(PlantingDate);
        }
    }

    private string _MoonPhase;
    public string MoonPhase
    {
        get => GetPathImageMoonPhase(_MoonPhase);
        set => Set(ref _MoonPhase, value);
    }

    private int _Germinate;
    public int Germinate
    {
        get => _Germinate;
        set => Set(ref _Germinate, value);
    }

    private DateTime _GerminationDate = DateTime.Now;
    public DateTime GerminationDate
    {
        get => _GerminationDate;
        set => Set(ref _GerminationDate, value);
    }

    private DateTime _ReplantsDate = DateTime.Now;
    public DateTime ReplantsDate
    {
        get => _ReplantsDate;
        set => Set(ref _ReplantsDate, value);
    }

    private bool _IsHaving = true;
    public bool IsHaving
    {
        get => _IsHaving;
        set
        {
            Set(ref _IsHaving, value);
            SelectedItem = null;
            SeedlingsView?.Refresh();
        }
    }

    private int _DeadNumbers;
    public int DeadNumbers
    {
        get => _DeadNumbers;
        set => Set(ref _DeadNumbers, value <= GetDeadSeedlingInfosCount()
            ? value
            : GetDeadSeedlingInfosCount());
    }

    private int GetDeadSeedlingInfosCount() =>
        SelectedItem.SeedlingInfos.Count - SelectedItem.SeedlingInfos.Count(d => d.IsDead == true) - 1;

    private string _DeathNote;
    public string DeathNote
    {
        get => _DeathNote;
        set => Set(ref _DeathNote, value);
    }

    // Методы

    private async Task LoadSeedling()
    {
        Seedlings = new ObservableCollection<Seedling>(await _SeedlingsService.Seedlings.ToArrayAsync().ConfigureAwait(false));
        _SeedlingsView.Source = _SeedlingsService.Seedlings.AsEnumerable()
            .Select(CreateSeedlingFromViewModel)
            .SortSeedlings();
        OnPropertyChanged(nameof(SeedlingsView));
    }

    private void LoadListCulture()
    {
        var listCultureQuery = _SeedlingsService.Seedlings
            .Select(seedlings => seedlings.Plant.PlantCulture.Name)
            .Distinct()
            .OrderBy(s => s);
        ListCulture.AddRange(listCultureQuery.ToListAsync().Result);
        var addListCulture = _SeedsService.Seeds
            .Select(seeds => new CultureFromViewModel
            {
                Id = seeds.Plant.PlantCulture.Id,
                Name = seeds.Plant.PlantCulture.Name
            })
            .AsEnumerable()
            .Distinct(s => s.Name)
            .OrderBy(s => s.Name);
        AddCultureList.AddRange(addListCulture.ToList());
        _CultureListView.Source = AddCultureList;
        OnPropertyChanged(nameof(CultureListView));
    }

    private void LoadListPlant()
    {
        var addListPlant = _SeedsService.Seeds
            .Where(seed => seed.SeedsInfo.AmountSeeds != 0 || seed.SeedsInfo.AmountSeedsWeight != 0)
            .Select(seeds => new PlantFromViewModel
            {
                Id = seeds.Id,
                Culture = seeds.Plant.PlantCulture.Name,
                Sort = seeds.Plant.PlantSort.Name,
                Producer = seeds.Plant.PlantSort.Producer.Name,
                ExpirationDate = seeds.SeedsInfo.ExpirationDate
            }).AsEnumerable()
            .OrderBy(s => s.Culture);
        AddPlantList.Add(addListPlant.ToList());
        _PlantListView.Source = AddPlantList;
        OnPropertyChanged(nameof(PlantListView));
    }

    private void LoadListSort()
    {
        var addListSort = _SeedsService.Seeds
            .Where(seed => seed.SeedsInfo.AmountSeeds != 0 || seed.SeedsInfo.AmountSeedsWeight != 0)
            .Select(seeds => new SortFromSeedlingsViewModel
            {
                Id = seeds.Plant.PlantSort.Id,
                Sort = seeds.Plant.PlantSort.Name,
                Culture = seeds.Plant.PlantCulture.Name
            }).AsEnumerable()
            .Distinct(s => s.Sort)
            .OrderBy(s => s.Sort);
        AddSortList.Add(addListSort.ToList());
        _SortListView.Source = AddSortList;
        OnPropertyChanged(nameof(SortListView));
    }

    private bool Verification() =>
        PlantingDate != default
        && AddSize != string.Empty
        && AddCulture != string.Empty
        && AddProducer != string.Empty
        && AddQuantity > 0.0
        && AddSort != string.Empty
        && SeedlingSource != string.Empty;

    private Plant GetPlant() => _SeedsService.Seeds.First(s => s.Id == CurrentPlant.Id).Plant;

    private void ClearFieldSeedlingView()
    {
        PlantingDate = DateTime.Now;
        AddSize = string.Empty;
        IsSeeds = true;
        AddSort = string.Empty;
        AddProducer = string.Empty;
        CurrentSeed = null;
        CurrentPlant = null;
        Plantable = null;
        AddQuantity = 0.0;
        AddQuantityString = string.Empty;
    }

    private void UpdateCollectionViewSource(int id = -1)
    {
        var collection = GetSeedlingFromViewModels().ToArray();
        _SeedlingsView.Source = collection;
        if (id != -1)
        {
            var current = collection.FirstOrDefault(s => s.Id == id);
            _SeedlingsView.View.MoveCurrentTo(current);
        }
        OnPropertyChanged(nameof(SeedlingsView));
    }

    private static string GetPathImageMoonPhase(string moonPhase) => moonPhase switch
    {
        "Растущий серп" => "Image/MoonPhase_2.jpg",
        "Первая четверть" => "Image/MoonPhase_3.jpg",
        "Растущая луна" => "Image/MoonPhase_4.jpg",
        "Полнолуние" => "Image/MoonPhase_5.jpg",
        "Убывающая луна" => "Image/MoonPhase_6.jpg",
        "Третья четверть" => "Image/MoonPhase_7.jpg",
        "Убывающий месяц" => "Image/MoonPhase_8.jpg",
        _ => "Image/MoonPhase_1.jpg"
    };

    private SeedlingFromViewModel CreateSeedlingFromViewModel(Seedling seedling)
    {
        var firstSeedlingInfo = seedling.SeedlingInfos.FirstOrDefault();
        return new SeedlingFromViewModel
        {
            Id = seedling.Id,
            Culture = seedling.Plant.PlantCulture.Name,
            Sort = seedling.Plant.PlantSort.Name,
            Producer = seedling.Plant.PlantSort.Producer.Name,
            Weight = seedling.Weight,
            Quantity = seedling.Quantity,
            LandingData = firstSeedlingInfo?.LandingDate ?? default,
            IsDead = firstSeedlingInfo?.IsDead ?? false,
            ReplantingData = firstSeedlingInfo?.Replants?.FirstOrDefault()?.ReplantingDate,
            SeedlingMoonPhase = firstSeedlingInfo != null ? GetPathImageMoonPhase(_SeedlingsService.Lunar.GetMoonPhase(firstSeedlingInfo.LandingDate)) : null,
            SeedlingInfos = firstSeedlingInfo != null
                ? new ObservableCollection<SeedlingInfoFromViewModel>(
                    seedling.SeedlingInfos.Skip(1).Select(info => new SeedlingInfoFromViewModel
                    {
                        Id = info.Id,
                        Number = info.SeedlingNumber,
                        GerminationData = info.GerminationDate,
                        QuenchingDate = info.QuenchingDate,
                        IsDead = info.IsDead,
                        IsQuarantine = info.QuarantineStartDate != null && info.QuarantineStopDate == null
                    }))
                : new ObservableCollection<SeedlingInfoFromViewModel>()
        };
    }

    private IOrderedEnumerable<SeedlingFromViewModel> GetSeedlingFromViewModels() =>
        Seedlings.Select(CreateSeedlingFromViewModel).SortSeedlings();

    private IOrderedEnumerable<SeedlingFromViewModel> GetSortedSeedlingFromViewModels(object p) =>
        Seedlings.Where(s => s.Plant.PlantCulture.Class == p.ToString())
            .Select(CreateSeedlingFromViewModel)
            .SortSeedlings();

    private void CopySeedlingToEditItem(Seedling seedlingFrom, Seedling seedlingTo)
    {
        if (seedlingFrom == null) return;

        seedlingTo.Id = seedlingFrom.Id;
        seedlingTo.Weight = seedlingFrom.Weight;
        seedlingTo.Quantity = seedlingFrom.Quantity;
        seedlingTo.SeedId = seedlingFrom.SeedId;

        seedlingTo.Plant.Id = seedlingFrom.Plant.Id;
        seedlingTo.Plant.PlantCulture.Id = seedlingFrom.Plant.PlantCulture.Id;
        seedlingTo.Plant.PlantCulture.Name = seedlingFrom.Plant.PlantCulture.Name;
        seedlingTo.Plant.PlantCulture.Class = seedlingFrom.Plant.PlantCulture.Class;
        seedlingTo.Plant.PlantSort.Id = seedlingFrom.Plant.PlantSort.Id;
        seedlingTo.Plant.PlantSort.Name = seedlingFrom.Plant.PlantSort.Name;
        seedlingTo.Plant.PlantSort.Description = seedlingFrom.Plant.PlantSort.Description;
        seedlingTo.Plant.PlantSort.MinGerminationTime = seedlingFrom.Plant.PlantSort.MinGerminationTime;
        seedlingTo.Plant.PlantSort.MaxGerminationTime = seedlingFrom.Plant.PlantSort.MaxGerminationTime;
        seedlingTo.Plant.PlantSort.AgeOfSeedlings = seedlingFrom.Plant.PlantSort.AgeOfSeedlings;
        seedlingTo.Plant.PlantSort.GrowingSeason = seedlingFrom.Plant.PlantSort.GrowingSeason;
        seedlingTo.Plant.PlantSort.LandingPattern = seedlingFrom.Plant.PlantSort.LandingPattern;
        seedlingTo.Plant.PlantSort.PlantHeight = seedlingFrom.Plant.PlantSort.PlantHeight;
        seedlingTo.Plant.PlantSort.PlantColor = seedlingFrom.Plant.PlantSort.PlantColor;
        seedlingTo.Plant.PlantSort.Producer.Id = seedlingFrom.Plant.PlantSort.Producer.Id;
        seedlingTo.Plant.PlantSort.Producer.Name = seedlingFrom.Plant.PlantSort.Producer.Name;

        seedlingTo.SeedlingInfos = seedlingFrom.SeedlingInfos;

        OnPropertyChanged(nameof(EditedItem));
        OnPropertyChanged(nameof(SelectedItem));
    }

    private void CreateSeedlingReport()
    {
        var items = Seedlings.Select(s =>
        {
            var infos = s.SeedlingInfos.Skip(1).ToList();
            return new SeedlingReportItem(
                s.Plant.PlantCulture.Name,
                s.Plant.PlantSort.Name,
                s.Plant.PlantSort.Producer.Name,
                s.SeedlingInfos.FirstOrDefault()?.LandingDate.ToShortDateString() ?? string.Empty,
                s.Weight,
                s.Quantity,
                infos.Count,
                infos.Count - infos.Count(i => i.IsDead == true));
        }).OrderBy(s => s.Culture);
        _ReportService.CreateSeedlingsReport(items);
    }

    private void CreateFilteredSeedlingReport()
    {
        var items = SeedlingsView!
            .Cast<SeedlingFromViewModel>()
            .Select(s => new SeedlingReportItem(
                s.Culture ?? string.Empty,
                s.Sort ?? string.Empty,
                s.Producer ?? string.Empty,
                s.LandingData?.ToShortDateString() ?? string.Empty,
                s.Weight ?? 0,
                s.Quantity ?? 0,
                s.CountGerminate,
                s.Balance ?? 0))
            .OrderBy(s => s.Culture);
        _ReportService.CreateSeedlingsReport(items, "Рассада_выборка");
    }

    // Команды

    private ICommand _LoadDataCommand;
    public ICommand LoadDataCommand => _LoadDataCommand
        ??= new LambdaCommandAsync(OnLoadDataCommandExecuted);

    private async Task OnLoadDataCommandExecuted()
    {
        if (Seedlings != null) return;
        await LoadSeedling();
        LoadListCulture();
        LoadListPlant();
        LoadListSort();
        AddProducer = null;
        AddSort = null;
        PlantingDate = DateTime.Now;
    }

    private ICommand _SeedlingsChoiceClassCommand;
    public ICommand SeedlingsChoiceClassCommand => _SeedlingsChoiceClassCommand
        ??= new LambdaCommandAsync(OnSeedlingsChoiceClassCommandExecuted);

    private async Task OnSeedlingsChoiceClassCommandExecuted(object p)
    {
        var seedlingsQuery = p.ToString() != "Выбрать все"
            ? GetSortedSeedlingFromViewModels(p)
            : GetSeedlingFromViewModels();
        _SeedlingsView.Source = seedlingsQuery.ToArray();
        OnPropertyChanged(nameof(SeedlingsView));
    }

    private ICommand _AddOrCorrectSeedlingCommand;
    public ICommand AddOrCorrectSeedlingCommand => _AddOrCorrectSeedlingCommand
        ??= new LambdaCommandAsync(OnAddOrCorrectSeedlingCommandExecuted, Verification);

    private async Task OnAddOrCorrectSeedlingCommandExecuted()
    {
        var plant = GetPlant();
        var seedlingInfo = new SeedlingInfo
        {
            LandingDate = PlantingDate,
            LunarPhase = _SeedlingsService.Lunar.GetMoonPhase(PlantingDate),
            SeedlingNumber = 0,
            SeedlingSource = SeedlingSource
        };

        var seedling = new Seedling
        {
            Plant = plant,
            SeedId = CurrentSeed.Id,
            SeedlingInfos = [seedlingInfo]
        };

        switch (AddSize)
        {
            case "гр.":
                seedling.Weight = AddQuantity;
                CurrentSeed.SeedsInfo.AmountSeedsWeight -= AddQuantity;
                break;
            case "шт.":
                seedling.Quantity = AddQuantity;
                CurrentSeed.SeedsInfo.AmountSeeds -= AddQuantity;
                break;
        }

        seedling = await _SeedlingsService.MakeASeedling(seedling).ConfigureAwait(false);
        Seedlings.Add(seedling);
        var seed = await _SeedsService.UpdateSeed(CurrentSeed).ConfigureAwait(false);
        RemoveItemFromCollection(seed);
        ClearFieldSeedlingView();
        UpdateCollectionViewSource(seedling.Id);
    }

    private void RemoveItemFromCollection(Seed seed)
    {
        if (seed.SeedsInfo.AmountSeeds != 0) return;
        if (seed.SeedsInfo.AmountSeedsWeight != null)
            if (seed.SeedsInfo.AmountSeeds + seed.SeedsInfo.AmountSeedsWeight != 0) return;
        AddPlantList.Remove(AddPlantList.First(s => s.Producer == seed.Plant.PlantSort.Producer.Name
                                                    && s.Culture == seed.Plant.PlantCulture.Name
                                                    && s.Sort == seed.Plant.PlantSort.Name
                                                    && s.ExpirationDate == seed.SeedsInfo.ExpirationDate));
        _PlantListView.Source = AddPlantList;
        var list = AddPlantList.Select(s => s).Where(s => s.Producer == seed.Plant.PlantSort.Producer.Name
                                                          && s.Culture == seed.Plant.PlantCulture.Name
                                                          && s.Sort == seed.Plant.PlantSort.Name).ToList();
        if (list.Count == 0)
            AddSortList.Remove(AddSortList.First(s => s.Sort == seed.Plant.PlantSort.Name));
        OnPropertyChanged(nameof(PlantListView));
        OnPropertyChanged(nameof(SortListView));
    }

    private ICommand _DeleteSeedlingCommand;
    public ICommand DeleteSeedlingCommand => _DeleteSeedlingCommand
        ??= new LambdaCommandAsync(OnDeleteSeedlingCommandExecuted, () => SelectedItem != null);

    private async Task OnDeleteSeedlingCommandExecuted()
    {
        if (!_UserDialog.YesNoQuestion(
                $"Вы уверены, что хотите удалить рассаду сорта - {SelectedItem.Plant.PlantSort.Name}",
                "Удаление рассады")) return;

        var seedId = SelectedItem.SeedId;
        var deleteSeedling = await _SeedlingsService.DeleteSeedling(SelectedItem).ConfigureAwait(false);
        await _SeedsService.ReturnSeedsFromSeedling(seedId, deleteSeedling!.Quantity, deleteSeedling.Weight).ConfigureAwait(false);
        Seedlings.Remove(deleteSeedling!);
        UpdateCollectionViewSource();
    }

    private ICommand _GerminateSeedlingCommand;
    public ICommand GerminateSeedlingCommand => _GerminateSeedlingCommand
        ??= new LambdaCommandAsync(OnGerminateSeedlingCommandExecuted, () => SelectedItem != null);

    private async Task OnGerminateSeedlingCommandExecuted()
    {
        switch (Germinate)
        {
            case < 0:
                return;
            case 0:
                if (SelectedItem.SeedlingInfos.Count > 1) return;
                SelectedItem.SeedlingInfos[0].IsDead = true;
                await _SeedlingsService.UpdateSeedlingInfo(SelectedItem.SeedlingInfos[0]).ConfigureAwait(false);
                UpdateCollectionViewSource();
                return;
        }

        if (SelectedItem.Quantity > 0)
            Germinate = Germinate + SelectedItem.SeedlingInfos.Count - 1 > (int)SelectedItem.Quantity
                ? (int)SelectedItem.Quantity - (SelectedItem.SeedlingInfos.Count - 1)
                : Germinate;

        for (var i = 0; i < Germinate; i++)
        {
            var info = new SeedlingInfo
            {
                GerminationDate = GerminationDate,
                LandingDate = SelectedItem.SeedlingInfos[0].LandingDate,
                LunarPhase = SelectedItem.SeedlingInfos[0].LunarPhase,
                SeedlingNumber = SelectedItem.SeedlingInfos.Count - 1 + 1,
                SeedlingSource = SelectedItem.SeedlingInfos[0].SeedlingSource
            };
            SelectedItem.SeedlingInfos.Add(info);
            await _SeedlingsService.AddSeedlingInfo(info);
        }

        await _SeedlingsService.UpdateSeedling(SelectedItem).ConfigureAwait(false);
        UpdateCollectionViewSource(SelectedSeedlingViewItem.Id);
        Germinate = 0;
        GerminationDate = DateTime.Now;
    }

    private ICommand _ReplantsSeedlingCommand;
    public ICommand ReplantsSeedlingCommand => _ReplantsSeedlingCommand
        ??= new LambdaCommandAsync(OnReplantsSeedlingCommandExecuted,
            () => SelectedItem != null && (SelectedItem.SeedlingInfos[0].Replants?.Count == 0 || SelectedItem.SeedlingInfos[0].Replants == null));

    private async Task OnReplantsSeedlingCommandExecuted()
    {
        SelectedItem.SeedlingInfos[0].Replants =
        [
            new Replanting { ReplantingDate = ReplantsDate }
        ];
        await _SeedlingsService.UpdateSeedlingInfo(SelectedItem.SeedlingInfos[0]);
        await _SeedlingsService.UpdateSeedling(SelectedItem).ConfigureAwait(false);
        UpdateCollectionViewSource(SelectedSeedlingViewItem.Id);
        ReplantsDate = DateTime.Now;
    }

    private ICommand _CreateSeedlingsReportCommand;
    public ICommand CreateSeedlingsReportCommand => _CreateSeedlingsReportCommand
        ??= new LambdaCommand(CreateSeedlingReport, () => Seedlings != null && IsActive);

    private ICommand _CreateFilteredSeedlingsReportCommand;
    public ICommand CreateFilteredSeedlingsReportCommand => _CreateFilteredSeedlingsReportCommand
        ??= new LambdaCommand(CreateFilteredSeedlingReport, () => SeedlingsView != null && IsActive);

    private ICommand _UpdateSeedlingInfoCommand;
    public ICommand UpdateSeedlingInfoCommand => _UpdateSeedlingInfoCommand
        ??= new LambdaCommandAsync(OnUpdateSeedlingInfoCommandExecuted, () => EditedItem != null);

    private async Task OnUpdateSeedlingInfoCommandExecuted()
    {
        if (!_UserDialog.YesNoQuestion(
                $"Вы уверены, что хотите изменить информацию о рассаде сорта - {EditedItem.Plant.PlantSort.Name}",
                "Редактирование рассады")) return;

        CopySeedlingToEditItem(EditedItem, SelectedItem);
        await _SeedlingsService.UpdateSeedling(SelectedItem).ConfigureAwait(false);
        UpdateCollectionViewSource(SelectedItem.Id);
    }

    private ICommand _CancelUpdateSeedlingInfoCommand;
    public ICommand CancelUpdateSeedlingInfoCommand => _CancelUpdateSeedlingInfoCommand
        ??= new LambdaCommandAsync(async () => UpdateCollectionViewSource(SelectedItem.Id));

    private ICommand _DeathSeedlingCommand;
    public ICommand DeathSeedlingCommand => _DeathSeedlingCommand
        ??= new LambdaCommandAsync(OnDeathSeedlingCommandExecuted,
            () => SelectedItem != null && SelectedItem.SeedlingInfos.Count > 1);

    private async Task OnDeathSeedlingCommandExecuted()
    {
        _SeedlingsService.InvertAutoSave();
        var dead = SelectedItem.SeedlingInfos.Count(d => d.IsDead == true);
        for (var i = SelectedItem.SeedlingInfos.Count - dead; i > SelectedItem.SeedlingInfos.Count - dead - DeadNumbers; i--)
        {
            SelectedItem.SeedlingInfos[i - 1].IsDead = true;
            SelectedItem.SeedlingInfos[i - 1].DeathNote = DeathNote;
            if (i == SelectedItem.SeedlingInfos.Count - dead - DeadNumbers + 1)
                _SeedlingsService.InvertAutoSave();
            await _SeedlingsService.UpdateSeedlingInfo(SelectedItem.SeedlingInfos[i - 1]);
        }

        await _SeedlingsService.UpdateSeedling(SelectedItem).ConfigureAwait(false);
        UpdateCollectionViewSource(SelectedSeedlingViewItem.Id);
        DeathNote = string.Empty;
        DeadNumbers = 0;
    }
}
