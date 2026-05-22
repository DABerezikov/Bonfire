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
using Bonfire.Models.Mappers;
using Bonfire.Services.Extensions;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;
using BonfireDB.Entities;

namespace Bonfire.ViewModels;

public class SeedlingsViewModel : SourceSelectionViewModel
{
    private readonly ISeedlingsService _seedlingsService;
    private readonly ISeedsService _seedsService;
    private readonly IUserDialog _userDialog;
    private readonly IReportService _reportService;

    // Кэш загруженных семян: источник для списков и для выбора при создании рассады.
    private List<Seed> _seeds = [];

    public SeedlingsViewModel(ISeedlingsService seedlings, ISeedsService seedsService, IUserDialog dialog, IReportService reportService)
    {
        _seedlingsService = seedlings;
        _seedsService = seedsService;
        _userDialog = dialog;
        _reportService = reportService;

        _seedlingsView = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(SeedlingFromViewModel.LandingData), ListSortDirection.Ascending),
                new SortDescription(nameof(SeedlingFromViewModel.Culture), ListSortDirection.Ascending),
                new SortDescription(nameof(SeedlingFromViewModel.Sort), ListSortDirection.Ascending),
                new SortDescription(nameof(SeedlingFromViewModel.Producer), ListSortDirection.Ascending)
            }
        };
        _seedlingsView.Filter += SeedsViewSource_Filter;

        _plantListView = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(PlantFromViewModel.Culture), ListSortDirection.Ascending),
                new SortDescription(nameof(PlantFromViewModel.Sort), ListSortDirection.Ascending),
                new SortDescription(nameof(PlantFromViewModel.Producer), ListSortDirection.Ascending)
            }
        };
        _plantListView.Filter += PlantListView_Filter;

        _cultureListView = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(CultureFromViewModel.Name), ListSortDirection.Ascending)
            }
        };
        _cultureListView.Filter += CultureListView_Filter;

        _sortListView = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(SortFromSeedlingsViewModel.Sort), ListSortDirection.Ascending)
            }
        };
        _sortListView.Filter += SortListView_Filter;
    }

    public bool IsActive
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            if (value && Seedlings != null)
                _ = RefreshSeedlingsAsync();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private async Task RefreshSeedlingsAsync()
    {
        Seedlings = new ObservableCollection<Seedling>(
            await _seedlingsService.GetAllSeedlingsAsync());
        UpdateCollectionViewSource();
    }

    // Фильтрация главного списка

    public ICollectionView? SeedlingsView => _seedlingsView.View;
    private readonly CollectionViewSource _seedlingsView;

    public string SeedlingFilter
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;

            SelectedItem = null;
            SeedlingsView?.Refresh();
        }
    } = "-Выбрать все-";

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

    public ObservableCollection<Seedling>? Seedlings
    {
        get;
        set => Set(ref field, value);
    }

    public SeedlingFromViewModel? SelectedSeedlingViewItem
    {
        get;
        set
        {
            Set(ref field, value);
            SelectedItem = value is null ? null : Seedlings!.First(s => s.Id == value.Id);
        }
    }

    public Seedling? SelectedItem
    {
        get;
        set
        {
            if (Set(ref field, value))
                CopySeedlingToEditItem(SelectedItem, EditedItem);
        }
    }

    public Seedling EditedItem
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
        SeedlingInfos =
        [
            new SeedlingInfo
            {
                Replants = [],
                Treatments = []
            }
        ]
    };

    public List<string> ListCulture
    {
        get;
        set => Set(ref field, value);
    } = ["-Выбрать все-"];

    // Источник рассады (радио-кнопки) — общая часть в SourceSelectionViewModel

    internal string SeedlingSource
    {
        get => SourceValue;
        set => SourceValue = value;
    }

    protected override void OnSourceChanged() => OnPropertyChanged(nameof(IsSeeds));

    public bool IsSeeds
    {
        get => SourceValue == PlantSources.FromSeeds;
        set { if (value) SourceValue = PlantSources.FromSeeds; }
    }

    // Единица измерения

    public string AddSize
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    // Выбор растения

    public ICollectionView? PlantListView => _plantListView.View;
    private readonly CollectionViewSource _plantListView;

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

    public ObservableCollection<PlantFromViewModel> AddPlantList
    {
        get;
        set => Set(ref field, value);
    } = [];

    public string AddProducer
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            if (!string.IsNullOrWhiteSpace(AddCulture) && !string.IsNullOrWhiteSpace(AddSort))
            {
                if (string.IsNullOrWhiteSpace(AddProducer)) return;
                CurrentPlant = AddPlantList.First(p => p.Producer + " " + p.ExpirationDate.Year == value
                                                       && p.Culture == AddCulture
                                                       && p.Sort == AddSort);
            }

            PlantListView?.Refresh();
        }
    } = string.Empty;

    public PlantFromViewModel? CurrentPlant
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            if (value is null) return;
            CurrentSeed = _seeds.First(s => s.Id == CurrentPlant!.Id);
        }
    }

    public Seed? CurrentSeed
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            if (value is null) return;
            Plantable = CurrentSeed!.SeedsInfo.AmountSeedsWeight > 0.0
                ? CurrentSeed.SeedsInfo.AmountSeedsWeight
                : CurrentSeed.SeedsInfo.AmountSeeds;
            AddSize = CurrentSeed.SeedsInfo.AmountSeedsWeight > 0.0 ? Units.GramsAbbr : Units.PiecesAbbr;
        }
    }

    public double? Plantable
    {
        get;
        set => Set(ref field, value);
    }

    // Выбор культуры

    public ICollectionView? CultureListView => _cultureListView.View;
    private readonly CollectionViewSource _cultureListView;

    private void CultureListView_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not CultureFromViewModel culture || string.IsNullOrEmpty(AddCulture)) return;
        if (!culture.Name!.Contains(AddCulture, StringComparison.OrdinalIgnoreCase))
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
            if (!Set(ref field, value)) return;
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
    } = string.Empty;

    // Выбор сорта

    public ICollectionView? SortListView => _sortListView.View;
    private readonly CollectionViewSource _sortListView;

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

    public ObservableCollection<SortFromSeedlingsViewModel> AddSortList
    {
        get;
        set => Set(ref field, value);
    } = [];

    public string AddSort
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            if (string.IsNullOrWhiteSpace(AddCulture) && string.IsNullOrWhiteSpace(AddSort)) return;
            var list = AddPlantList.Select(p => p).Where(p => p.Culture == AddCulture && p.Sort == AddSort).ToList();
            if (list.Count == 1)
                AddProducer = AddPlantList.First(p => p.Culture == AddCulture && p.Sort == AddSort).ToString();
            SortListView?.Refresh();
            PlantListView?.Refresh();
        }
    } = string.Empty;

    // Куда посажено (при добавлении рассады вручную)

    public string AddPlantPlace
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    // Количество и даты

    public string AddQuantityString
    {
        get;
        set
        {
            Set(ref field, value);
            AddQuantity = AddQuantityString.DoubleParseAdvanced();
        }
    } = string.Empty;

    public double AddQuantity
    {
        get;
        set
        {
            if (value > Plantable) value = (double)Plantable;
            Set(ref field, value);
        }
    }

    public DateTime PlantingDate
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;
            MoonPhase = _seedlingsService.Lunar.GetMoonPhase(PlantingDate);
        }
    }

    public string MoonPhase
    {
        get => GetPathImageMoonPhase(field ?? string.Empty);
        set => Set(ref field, value);
    } = string.Empty;

    public int Germinate
    {
        get;
        set => Set(ref field, value);
    }

    public DateTime GerminationDate
    {
        get;
        set => Set(ref field, value);
    } = DateTime.Now;

    public DateTime ReplantsDate
    {
        get;
        set => Set(ref field, value);
    } = DateTime.Now;

    public bool IsHaving
    {
        get;
        set
        {
            Set(ref field, value);
            SelectedItem = null;
            SeedlingsView?.Refresh();
        }
    } = true;

    public int DeadNumbers
    {
        get;
        set => Set(ref field, value <= GetDeadSeedlingInfosCount()
            ? value
            : GetDeadSeedlingInfosCount());
    }

    private int GetDeadSeedlingInfosCount()
    {
        var germinations = SelectedItem!.SeedlingInfos.Where(i => i.GerminationDate.HasValue).ToList();
        return germinations.Count - germinations.Count(d => d.IsDead == true);
    }

    public string DeathNote
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    // Методы

    private async Task LoadSeedling()
    {
        var seedlings = await _seedlingsService.GetAllSeedlingsAsync();
        Seedlings = new ObservableCollection<Seedling>(seedlings);
        _seedlingsView.Source = seedlings
            .Select(CreateSeedlingFromViewModel)
            .SortSeedlings();
        OnPropertyChanged(nameof(SeedlingsView));
    }

    private void LoadListCulture()
    {
        var listCulture = Seedlings!
            .Select(s => s.Plant.PlantCulture.Name)
            .Distinct()
            .OrderBy(s => s);
        ListCulture.AddRange(listCulture);

        var addListCulture = _seeds
            .Select(s => new CultureFromViewModel
            {
                Id = s.Plant.PlantCulture.Id,
                Name = s.Plant.PlantCulture.Name
            })
            .Distinct(s => s!.Name)
            .OrderBy(s => s.Name);
        AddCultureList.AddRange(addListCulture.ToList());
        _cultureListView.Source = AddCultureList;
        OnPropertyChanged(nameof(CultureListView));
    }

    // Списки сортов/растений строятся из кэша семян (только с ненулевым остатком).
    private void LoadListPlant()
    {
        var plants = _seeds
            .Where(seed => seed.SeedsInfo.AmountSeeds != 0 || seed.SeedsInfo.AmountSeedsWeight != 0)
            .Select(s => new PlantFromViewModel
            {
                Id = s.Id,
                Culture = s.Plant.PlantCulture.Name,
                Sort = s.Plant.PlantSort.Name,
                Producer = s.Plant.PlantSort.Producer.Name,
                ExpirationDate = s.SeedsInfo.ExpirationDate
            })
            .OrderBy(s => s.Culture);
        AddPlantList.AddRange(plants.ToList());
        _plantListView.Source = AddPlantList;
        OnPropertyChanged(nameof(PlantListView));
    }

    private void LoadListSort()
    {
        var addListSort = _seeds
            .Where(seed => seed.SeedsInfo.AmountSeeds != 0 || seed.SeedsInfo.AmountSeedsWeight != 0)
            .Select(s => new SortFromSeedlingsViewModel
            {
                Id = s.Plant.PlantSort.Id,
                Sort = s.Plant.PlantSort.Name,
                Culture = s.Plant.PlantCulture.Name
            })
            .Distinct(s => s!.Sort + "|" + s.Culture)
            .OrderBy(s => s.Sort);
        AddSortList.AddRange(addListSort.ToList());
        _sortListView.Source = AddSortList;
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

    private Plant GetPlant() => _seeds.First(s => s.Id == CurrentPlant!.Id).Plant;

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
        AddPlantPlace = string.Empty;
    }

    private void UpdateCollectionViewSource(int id = -1)
    {
        var collection = GetSeedlingFromViewModels().ToArray();
        _seedlingsView.Source = collection;
        if (id != -1)
        {
            var current = collection.FirstOrDefault(s => s.Id == id);
            _seedlingsView.View.MoveCurrentTo(current);
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
        // Метаданные: предпочитаем поля на Seedling (новый стиль), иначе берём из SeedlingInfo[0]
        var metaInfo   = seedling.SeedlingInfos.FirstOrDefault(i => !i.GerminationDate.HasValue);
        var landingDate = seedling.LandingDate ?? metaInfo?.LandingDate;
        var moonPhaseStr = seedling.LunarPhase ?? (metaInfo != null
            ? _seedlingsService.Lunar.GetMoonPhase(metaInfo.LandingDate)
            : string.Empty);

        // Только записи всходов (с датой); метаданные и "посажено в грунт" отфильтрованы
        var germinations = seedling.SeedlingInfos
            .Where(i => i.GerminationDate.HasValue)
            .Select(info => new SeedlingInfoFromViewModel
            {
                Id            = info.Id,
                Number        = info.SeedlingNumber,
                GerminationData = info.GerminationDate,
                QuenchingDate = info.QuenchingDate,
                IsDead        = info.IsDead,
                IsQuarantine  = info.QuarantineStartDate != null && info.QuarantineStopDate == null
            });

        return new SeedlingFromViewModel
        {
            Id             = seedling.Id,
            Culture        = seedling.Plant.PlantCulture.Name,
            Sort           = seedling.Plant.PlantSort.Name,
            Producer       = seedling.Plant.PlantSort.Producer.Name,
            Weight         = seedling.Weight,
            Quantity       = seedling.Quantity,
            PlantedOut     = seedling.PlantedOut,
            LandingData    = landingDate,
            IsDead         = metaInfo?.IsDead ?? false,
            ReplantingData = metaInfo?.Replants?.FirstOrDefault()?.ReplantingDate,
            SeedlingMoonPhase = GetPathImageMoonPhase(moonPhaseStr),
            SeedlingInfos  = new ObservableCollection<SeedlingInfoFromViewModel>(germinations)
        };
    }

    private IOrderedEnumerable<SeedlingFromViewModel> GetSeedlingFromViewModels() =>
        Seedlings!.Select(CreateSeedlingFromViewModel).SortSeedlings();

    private IOrderedEnumerable<SeedlingFromViewModel> GetSortedSeedlingFromViewModels(object p) =>
        Seedlings!.Where(s => s.Plant.PlantCulture.Class == p.ToString())
            .Select(CreateSeedlingFromViewModel)
            .SortSeedlings();

    private void CopySeedlingToEditItem(Seedling? seedlingFrom, Seedling seedlingTo)
    {
        if (seedlingFrom == null) return;
        SeedlingMapper.CopyInto(seedlingFrom, seedlingTo);
        OnPropertyChanged(nameof(EditedItem));
        OnPropertyChanged(nameof(SelectedItem));
    }

    private void CreateSeedlingReport()
    {
        var items = Seedlings!.Select(s =>
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
        _reportService.CreateSeedlingsReport(items);
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
        _reportService.CreateSeedlingsReport(items, "Рассада_выборка");
    }

    // Команды

    public ICommand LoadDataCommand => field
        ??= new LambdaCommandAsync(OnLoadDataCommandExecuted);

    private async Task OnLoadDataCommandExecuted()
    {
        if (Seedlings != null) return;
        _seeds = (await _seedsService.GetAllSeedsAsync()).ToList();
        await LoadSeedling();
        LoadListCulture();
        LoadListPlant();
        LoadListSort();
        AddProducer = string.Empty;
        AddSort = string.Empty;
        PlantingDate = DateTime.Now;
    }

    public ICommand SeedlingsChoiceClassCommand => field
        ??= new LambdaCommandAsync(OnSeedlingsChoiceClassCommandExecuted);

    private async Task OnSeedlingsChoiceClassCommandExecuted(object p)
    {
        var seedlingsQuery = p.ToString() != "Выбрать все"
            ? GetSortedSeedlingFromViewModels(p)
            : GetSeedlingFromViewModels();
        _seedlingsView.Source = seedlingsQuery.ToArray();
        OnPropertyChanged(nameof(SeedlingsView));
    }

    public ICommand AddOrCorrectSeedlingCommand => field
        ??= new LambdaCommandAsync(OnAddOrCorrectSeedlingCommandExecuted, Verification);

    private async Task OnAddOrCorrectSeedlingCommandExecuted()
    {
        var plant = GetPlant();
        var moonPhase  = _seedlingsService.Lunar.GetMoonPhase(PlantingDate);
        var plantPlace = string.IsNullOrWhiteSpace(AddPlantPlace) ? null : AddPlantPlace;

        var seedlingInfo = new SeedlingInfo
        {
            LandingDate    = PlantingDate,
            LunarPhase     = moonPhase,
            SeedlingNumber = 0,
            SeedlingSource = SeedlingSource,
            PlantPlace     = plantPlace
        };

        var seedling = new Seedling
        {
            Plant          = plant,
            SeedId         = CurrentSeed!.Id,
            LandingDate    = PlantingDate,
            LunarPhase     = moonPhase,
            SeedlingSource = SeedlingSource,
            PlantPlace     = plantPlace,
            SeedlingInfos  = [seedlingInfo]
        };

        switch (AddSize)
        {
            case Units.GramsAbbr:
                seedling.Weight = AddQuantity;
                CurrentSeed.SeedsInfo.AmountSeedsWeight -= AddQuantity;
                break;
            case Units.PiecesAbbr:
                seedling.Quantity = AddQuantity;
                CurrentSeed.SeedsInfo.AmountSeeds -= AddQuantity;
                break;
        }

        seedling = await _seedlingsService.MakeASeedling(seedling).ConfigureAwait(false);
        Seedlings!.Add(seedling);
        var seed = await _seedsService.UpdateSeed(CurrentSeed).ConfigureAwait(false);
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
        _plantListView.Source = AddPlantList;
        var list = AddPlantList.Select(s => s).Where(s => s.Producer == seed.Plant.PlantSort.Producer.Name
                                                          && s.Culture == seed.Plant.PlantCulture.Name
                                                          && s.Sort == seed.Plant.PlantSort.Name).ToList();
        if (list.Count == 0)
            AddSortList.Remove(AddSortList.First(s => s.Sort == seed.Plant.PlantSort.Name));
        OnPropertyChanged(nameof(PlantListView));
        OnPropertyChanged(nameof(SortListView));
    }

    public ICommand DeleteSeedlingCommand => field
        ??= new LambdaCommandAsync(OnDeleteSeedlingCommandExecuted, () => SelectedItem != null);

    private async Task OnDeleteSeedlingCommandExecuted()
    {
        if (!_userDialog.YesNoQuestion(
                $"Вы уверены, что хотите удалить рассаду сорта - {SelectedItem!.Plant.PlantSort.Name}",
                "Удаление рассады")) return;

        var seedId = SelectedItem.SeedId;
        var deleteSeedling = await _seedlingsService.DeleteSeedling(SelectedItem).ConfigureAwait(false);
        await _seedsService.ReturnSeedsFromSeedling(seedId, deleteSeedling!.Quantity, deleteSeedling.Weight).ConfigureAwait(false);
        Seedlings!.Remove(deleteSeedling!);
        UpdateCollectionViewSource();
    }

    public ICommand GerminateSeedlingCommand => field
        ??= new LambdaCommandAsync(OnGerminateSeedlingCommandExecuted, () => SelectedItem != null);

    private async Task OnGerminateSeedlingCommandExecuted()
    {
        switch (Germinate)
        {
            case < 0:
                return;
            case 0:
                if (SelectedItem!.SeedlingInfos.Count > 1) return;
                SelectedItem.SeedlingInfos[0].IsDead = true;
                await _seedlingsService.UpdateSeedlingInfo(SelectedItem.SeedlingInfos[0]).ConfigureAwait(false);
                UpdateCollectionViewSource();
                return;
        }

        var germCount = SelectedItem!.SeedlingInfos.Count(i => i.GerminationDate.HasValue);
        if (SelectedItem.Quantity > 0)
            Germinate = Germinate + germCount > (int)SelectedItem.Quantity
                ? (int)SelectedItem.Quantity - germCount
                : Germinate;

        for (var i = 0; i < Germinate; i++)
        {
            var info = new SeedlingInfo
            {
                GerminationDate = GerminationDate,
                SeedlingNumber  = germCount + i + 1
            };
            SelectedItem.SeedlingInfos.Add(info);
            await _seedlingsService.AddSeedlingInfo(info);
        }

        await _seedlingsService.UpdateSeedling(SelectedItem).ConfigureAwait(false);
        UpdateCollectionViewSource(SelectedSeedlingViewItem!.Id);
        Germinate = 0;
        GerminationDate = DateTime.Now;
    }

    public ICommand ReplantsSeedlingCommand => field
        ??= new LambdaCommandAsync(OnReplantsSeedlingCommandExecuted,
            () => SelectedItem != null && (SelectedItem.SeedlingInfos[0].Replants?.Count == 0 || SelectedItem.SeedlingInfos[0].Replants == null));

    private async Task OnReplantsSeedlingCommandExecuted()
    {
        SelectedItem!.SeedlingInfos[0].Replants =
        [
            new Replanting { ReplantingDate = ReplantsDate }
        ];
        await _seedlingsService.UpdateSeedlingInfo(SelectedItem.SeedlingInfos[0]);
        await _seedlingsService.UpdateSeedling(SelectedItem).ConfigureAwait(false);
        UpdateCollectionViewSource(SelectedSeedlingViewItem!.Id);
        ReplantsDate = DateTime.Now;
    }

    public ICommand CreateSeedlingsReportCommand => field
        ??= new LambdaCommand(CreateSeedlingReport, () => Seedlings != null && IsActive);

    public ICommand CreateFilteredSeedlingsReportCommand => field
        ??= new LambdaCommand(CreateFilteredSeedlingReport, () => SeedlingsView != null && IsActive);

    public ICommand UpdateSeedlingInfoCommand => field
        ??= new LambdaCommandAsync(OnUpdateSeedlingInfoCommandExecuted, () => EditedItem != null);

    private async Task OnUpdateSeedlingInfoCommandExecuted()
    {
        if (!_userDialog.YesNoQuestion(
                $"Вы уверены, что хотите изменить информацию о рассаде сорта - {EditedItem.Plant.PlantSort.Name}",
                "Редактирование рассады")) return;

        CopySeedlingToEditItem(EditedItem, SelectedItem!);
        await _seedlingsService.UpdateSeedling(SelectedItem!).ConfigureAwait(false);
        UpdateCollectionViewSource(SelectedItem!.Id);
    }

    public ICommand CancelUpdateSeedlingInfoCommand => field
        ??= new LambdaCommandAsync(async () => UpdateCollectionViewSource(SelectedItem?.Id ?? -1));

    public ICommand DeathSeedlingCommand => field
        ??= new LambdaCommandAsync(OnDeathSeedlingCommandExecuted,
            () => SelectedItem != null && SelectedItem.SeedlingInfos.Count > 1);

    private async Task OnDeathSeedlingCommandExecuted()
    {
        var toMark = SelectedItem!.SeedlingInfos
            .Where(i => i.GerminationDate.HasValue && i.IsDead != true)
            .OrderByDescending(i => i.SeedlingNumber)
            .Take(DeadNumbers)
            .ToList();

        await _seedlingsService.MarkSeedlingInfosDeadAsync(SelectedItem, toMark, DeathNote);

        UpdateCollectionViewSource(SelectedSeedlingViewItem!.Id);
        DeathNote = string.Empty;
        DeadNumbers = 0;
    }
}
