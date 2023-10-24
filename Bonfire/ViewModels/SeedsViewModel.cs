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
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;


namespace Bonfire.ViewModels;

public class SeedsViewModel : ViewModel
{
    private readonly ISeedsService _seedsService;
    private readonly IUserDialog _userDialog;

    
    
    public SeedsViewModel(ISeedsService seedsService, IUserDialog userDialog)
    {
        _seedsService = seedsService;
        _userDialog = userDialog;
        _SeedsView = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(SeedsFromViewModel.Culture), ListSortDirection.Ascending)

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
                new SortDescription(nameof(SortFromViewModel.Name), ListSortDirection.Ascending)
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

   


    #region Свойства

    #region FilterSeeds - Фильтрация по культуре

    public ICollectionView SeedsView => _SeedsView?.View;
    private readonly CollectionViewSource _SeedsView;

    #region SeedFilter : string - Искомое слово

    /// <summary>Искомое слово</summary>
    private string _SeedFilter = "Выбрать все";

    /// <summary>Искомое слово</summary>
    public string SeedFilter
    {
        get => _SeedFilter;
        set
        {
            if (Set(ref _SeedFilter, value))
            {
                SeedsView.Refresh();
            }
        } 
    }

    #endregion
    private void _SeedsViewSource_Filter(object sender, FilterEventArgs e)
    {
        if (!(e.Item is SeedsFromViewModel seed) || string.IsNullOrEmpty(SeedFilter) || SeedFilter == "Выбрать все") return;
        if (!seed.Culture.Contains(SeedFilter, StringComparison.OrdinalIgnoreCase))
            e.Accepted = false;
    }

    #endregion

    #region Seeds : ObservableCollection<Seed> - Коллекция семян

    /// <summary>Коллекция семян</summary>
    private ObservableCollection<Seed> _Seeds;

    /// <summary>Коллекция семян</summary>
    public ObservableCollection<Seed> Seeds
    {
        get => _Seeds;
        set => Set(ref _Seeds, value);
    }
    #endregion

    #region SelectedItem : SeedsFromViewModel - Выбранный объект

    /// <summary>Выбранный объект</summary>
    private SeedsFromViewModel _SelectedItem;

    /// <summary>Выбранный объект</summary>
    public SeedsFromViewModel SelectedItem
    {
        get => _SelectedItem;
        set => Set(ref _SelectedItem, value);
    }

    #endregion

    #region ListCulture : List<string> - Список культур

    /// <summary>Список культур</summary>
    private List<string> _ListCulture = new List<string> { "Выбрать все" };

    /// <summary>Список культур</summary>
    public List<string> ListCulture
    {
        get => _ListCulture;
        set => Set(ref _ListCulture, value);
    }


    #endregion

    #region ListSort : List<string> - Список сортов

    
    /// <summary>Список сортов</summary>
    private List<string> _ListSort = new List<string> { "Выбрать все" };

    /// <summary>Список сортов</summary>
    public List<string> ListSort
    {
        get => _ListSort;
        set => Set(ref _ListSort, value);
    } 
    #endregion
    
    #region ListProducer : List<string> - Список производителей

    
    /// <summary>Список производителей</summary>
    private List<string> _ListProducer = new List<string> { "Выбрать все" };

    /// <summary>Список производителей</summary>
    public List<string> ListProducer
    {
        get => _ListProducer;
        set => Set(ref _ListProducer, value);
    }
    #endregion

    #region AddQuantityInPac :string - Количество семян в упаковке


    /// <summary>Количество семян в упаковке</summary>
    private string _AddQuantityInPac = string.Empty;

    /// <summary>Количество семян в упаковке</summary>
    public string AddQuantityInPac
    {
        get => _AddQuantityInPac;
        set => Set(ref _AddQuantityInPac, value);
    } 
    #endregion
    
    #region AddQuantityPac :string - Количество упаковок


    /// <summary>Количество упаковок</summary>
    private string _AddQuantityPac = string.Empty;

    /// <summary>Количество упаковок</summary>
    public string AddQuantityPac
    {
        get => _AddQuantityPac;
        set => Set(ref _AddQuantityPac, value);
    }
    #endregion

    #region AddBestBy :DateTime - Срок годности семян


    /// <summary>Срок годности семян</summary>
    private DateTime _AddBestBy = DateTime.Now + TimeSpan.FromDays(365);

    /// <summary>Срок годности семян</summary>
    public DateTime AddBestBy
    {
        get => _AddBestBy;
        set => Set(ref _AddBestBy, value);
    }
    #endregion

    #region AddOrCorrectSeed :string - Количество упаковок


    /// <summary>Количество упаковок</summary>
    private string _AddOrCorrectSeed = "Добавить";

    /// <summary>Количество упаковок</summary>
    public string AddOrCorrectSeed
    {
        get => _AddOrCorrectSeed;
        set => Set(ref _AddOrCorrectSeed, value);
    }
    #endregion



    #endregion

    #region Логика кнопок выбора источника семян

    #region SeedSource : string - Результат выбора источника семян

    /// <summary>Результат выбора источника семян</summary>
    private string _SeedSource;

    /// <summary>Результат выбора источника семян</summary>
    private string SeedSource
    {
        get => _SeedSource;
        set
        {
            Set(ref _SeedSource, value);
            AddProducer = IsCollected ? "Свои семена" : "";
        } 
    }

    #endregion

    #region IsSold : bool - Выбор способа  получения семян - куплено

    /// <summary>Выбор способа  получения семян - куплено</summary>
    private bool _IsSold;

    /// <summary>Выбор способа  получения семян - куплено</summary>
    public bool IsSold
    {
        get => _IsSold;
        set
        {
            if (Set(ref _IsSold, value))
                SeedSource = "Куплено";
            
        } 
    }

    #endregion

    #region IsDonated : bool - Выбор способа  получения семян - подарено

    /// <summary>Выбор способа  получения семян - подарено</summary>
    private bool _IsDonated;

    /// <summary>Выбор способа  получения семян - подарено</summary>
    public bool IsDonated
    {
        get => _IsDonated;
        set
        {
            if (Set(ref _IsDonated, value))
                SeedSource = "Подарено";

        }
    }

    #endregion

    #region IsCollected : bool - Выбор способа  получения семян - собрано

    /// <summary>Выбор способа  получения семян - собрано</summary>
    private bool _IsCollected;

    /// <summary>Выбор способа  получения семян - собрано</summary>
    public bool IsCollected
    {
        get => _IsCollected;
        set
        {
            if (Set(ref _IsCollected, value))
                SeedSource = "Собрано";

        }
    }

    #endregion

    #endregion

    #region AddNote : string - Примечание при добавлении семян

    /// <summary>Примечание при добавлении семян</summary>
    private string _AddNote;

    /// <summary>Примечание при добавлении семян</summary>
    public string AddNote
    {
        get => _AddNote;
        set => Set(ref _AddNote, value);
    }

    #endregion

    #region Выбор единиц измерения для добавления семян

    #region AddSizeList : List<string> - Список единиц измерения

    /// <summary>Список единиц измерения</summary>
    private List<string> _AddSizeList = new() {"Граммы", "Штуки"};

    /// <summary>Список единиц измерения</summary>
    public List<string> AddSizeList
    {
        get => _AddSizeList;
        set => Set(ref _AddSizeList, value);
    }

    #endregion

    #region AddSize : string - Выбранная единица измерения для добавления семян

    /// <summary>Выбранная единица измерения для добавления семян</summary>
    private string _AddSize;

    /// <summary>Выбранная единица измерения для добавления семян</summary>
    public string AddSize
    {
        get => _AddSize;
        set => Set(ref _AddSize, value);
    }

    #endregion


    #endregion

    #region Выбор класса для добавления семян

    #region AddClassList : ObservableCollection<string> - Список классов семян

    /// <summary>Список классов семян</summary>
    private ObservableCollection<string> _AddClassList = new(PlantClassList.GetClassList());

    /// <summary>Список классов семян</summary>
    public ObservableCollection<string> AddClassList
    {
        get => _AddClassList;
        set => Set(ref _AddClassList, value);
    }

    #endregion

    #region AddClass : string - Выбранный класс

    /// <summary>Выбранный класс</summary>
    private string _AddClass;

    /// <summary>Выбранный класс</summary>
    public string AddClass
    {
        get => _AddClass;
        set => Set(ref _AddClass, value);
    }

    #endregion

    #endregion

    #region Выбор культуры для добавления семян

    public ICollectionView CultureListView  => _CultureListView?.View;
    private readonly CollectionViewSource _CultureListView;
    

    private void _CultureListView_Filter(object sender, FilterEventArgs e)
    {
        if (!(e.Item is CultureFromViewModel culture) || string.IsNullOrEmpty(AddCulture)) return;
        if (!culture.Name.Contains(AddCulture, StringComparison.OrdinalIgnoreCase))
            e.Accepted = false;
    }

    #region AddCultureList : List<string> - Список культур для добавления семян

    /// <summary>Список культур для добавления семян</summary>
    private ObservableCollection<CultureFromViewModel> _AddCultureList = new();

    /// <summary>Список культур для добавления семян</summary>
    public ObservableCollection<CultureFromViewModel> AddCultureList
    {
        get => _AddCultureList;
        set => Set(ref _AddCultureList, value);
    }

    #endregion

    #region AddCulture : string - Выбранная культура для добавления семян

    /// <summary>Выбранная культура для добавления семян</summary>
    private string _AddCulture;

    /// <summary>Выбранная культура для добавления семян</summary>
    public string AddCulture
    {
        get => _AddCulture;
        set
        {
            if (Set(ref _AddCulture, value))
                CultureListView.Refresh();
        } 
    }

    #endregion

    #endregion
    
    #region Выбор сорта для добавления семян

    public ICollectionView SortListView  => _SortListView?.View;
    private readonly CollectionViewSource _SortListView;
    

    private void _SortListView_Filter(object sender, FilterEventArgs e)
    {
        if (!(e.Item is SortFromViewModel culture) || string.IsNullOrEmpty(AddSort)) return;
        if (!culture.Name.Contains(AddSort, StringComparison.OrdinalIgnoreCase))
            e.Accepted = false;
    }

    #region AddSortList : List<string> - Список культур для добавления семян

    /// <summary>Список сортов для добавления семян</summary>
    private ObservableCollection<SortFromViewModel> _AddSortList = new();

    /// <summary>Список сортов для добавления семян</summary>
    public ObservableCollection<SortFromViewModel> AddSortList
    {
        get => _AddSortList;
        set => Set(ref _AddSortList, value);
    }

    #endregion

    #region AddSort : string - Выбранный сорт для добавления семян

    /// <summary>Выбранный сорт для добавления семян</summary>
    private string _AddSort;

    /// <summary>Выбранный сорт для добавления семян</summary>
    public string AddSort
    {
        get => _AddSort;
        set
        {
            if (Set(ref _AddSort, value))
                SortListView.Refresh();
        } 
    }

    #endregion
    
    #endregion
    
    #region Выбор производителя для добавления семян

    public ICollectionView ProducerListView  => _ProducerListView?.View;
    private readonly CollectionViewSource _ProducerListView;
    

    private void _ProducerListView_Filter(object sender, FilterEventArgs e)
    {
        if (!(e.Item is ProducerFromViewModel culture) || string.IsNullOrEmpty(AddProducer)) return;
        if (!culture.Name.Contains(AddProducer, StringComparison.OrdinalIgnoreCase))
            e.Accepted = false;
    }

    #region AddProducerList : List<string> - Список культур для добавления семян

    /// <summary>Список сортов для добавления семян</summary>
    private ObservableCollection<ProducerFromViewModel> _AddProducerList = new();

    /// <summary>Список сортов для добавления семян</summary>
    public ObservableCollection<ProducerFromViewModel> AddProducerList
    {
        get => _AddProducerList;
        set => Set(ref _AddProducerList, value);
    }

    #endregion

    #region AddProducer : string - Выбранный сорт для добавления семян

    /// <summary>Выбранный сорт для добавления семян</summary>
    private string _AddProducer;

    /// <summary>Выбранный сорт для добавления семян</summary>
    public string AddProducer
    {
        get => _AddProducer;
        set
        {
            if (Set(ref _AddProducer, value))
                ProducerListView.Refresh();
        } 
    }

    #endregion

    #endregion


    


    

    #region Методы



    #region Метод загрузки семян

    private async Task LoadSeed()
    {
        var seedsQuery = _seedsService.Seeds
                .Select(seeds => new SeedsFromViewModel
                {
                    Id = seeds.Id,
                    Culture = seeds.Plant.PlantCulture.Name,
                    Sort = seeds.Plant.PlantSort.Name,
                    Producer = seeds.Plant.PlantSort.Producer.Name,
                    ExpirationDate = seeds.SeedsInfo.ExpirationDate,
                    QuantityPack = seeds.SeedsInfo.QuantityPack,
                    WeightPack = seeds.SeedsInfo.WeightPack,
                    AmountSeedsQuantity = seeds.SeedsInfo.AmountSeeds,
                    AmountSeedsWeight = seeds.SeedsInfo.AmountSeedsWeight
                })
            ;
        Seeds = new ObservableCollection<Seed>(await _seedsService.Seeds.ToArrayAsync());
        _SeedsView.Source = await seedsQuery.ToArrayAsync();
        OnPropertyChanged(nameof(SeedsView));
    }

    #endregion

    #region Метод загрузки списка культур

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
            })
            .Distinct()
            .OrderBy(s=>s.Name);
        ListCulture.AddRange(listCultureQuery.ToListAsync().Result.ToHashSet());
        AddCultureList.AddRange(addListCulture.ToListAsync().Result.ToHashSet());
        _CultureListView.Source = AddCultureList;
        OnPropertyChanged(nameof(CultureListView));

    }

    #endregion
    
    #region Метод загрузки списка сортов

    private void LoadListSort()
    {
        var listSortQuery = _seedsService.Seeds
            .Select(seeds => seeds.Plant.PlantSort.Name)
            .Distinct()
            .OrderBy(s => s);
        var addListSort = _seedsService.Seeds
            .Select(seeds => new SortFromViewModel
            {
                Id = seeds.Plant.PlantSort.Id,
                Name = seeds.Plant.PlantSort.Name
            })
            .Distinct()
            .OrderBy(s=>s.Name);
        ListSort.AddRange(listSortQuery.ToListAsync().Result.ToHashSet());
        AddSortList.AddRange(addListSort.ToListAsync().Result.ToHashSet());
        _SortListView.Source = AddSortList;
        OnPropertyChanged(nameof(SortListView));

    }

    #endregion
    
    #region Метод загрузки списка производителей

    private void LoadListProducer()
    {
        var listProducerQuery = _seedsService.Seeds
            .Select(seeds => seeds.Plant.PlantSort.Producer.Name)
            .Distinct()
            .OrderBy(s => s);
        var addListProducer = _seedsService.Seeds
            .Select(seeds => new ProducerFromViewModel
            {
                Id = seeds.Plant.PlantSort.Producer.Id,
                Name = seeds.Plant.PlantSort.Producer.Name
            })
            .Distinct()
            .OrderBy(s=>s.Name);
        ListProducer.AddRange(listProducerQuery.ToListAsync().Result.ToHashSet());
        AddProducerList.AddRange(addListProducer.ToListAsync().Result.ToHashSet());
        _ProducerListView.Source = AddProducerList;
        OnPropertyChanged(nameof(ProducerListView));

    }

    #endregion

    #region Метод для поиска или создания растения

    private Plant GetOrCreatePlant()
    {
       
        if (ListSort.Contains(AddSort) && ListCulture.Contains(AddCulture) && ListProducer.Contains(AddProducer))
        {
            var seed = _seedsService.Seeds
                .Find(s =>
                    s.Plant.PlantCulture.Name == AddCulture
                    && s.Plant.PlantSort.Name == AddSort
                    && s.Plant.PlantSort.Producer.Name == AddProducer);
                    
            return seed.Plant;
        }

        var plant = new Plant
        {
            PlantCulture = new PlantCulture
            {
                Name = AddCulture
            },
            PlantSort = new PlantSort
            {
                Name = AddSort,
                Producer = new Producer
                {
                    Name = AddProducer
                }
            }
        };

        return plant;
    }

    #endregion

    #endregion






    #region Команды

    #region Command LoadDataCommand - Команда для загрузки данных из репозитория

    /// <summary> Команда для загрузки данных из репозитория </summary>
    private ICommand _LoadDataCommand;

    /// <summary> Команда для загрузки данных из репозитория </summary>
    public ICommand LoadDataCommand => _LoadDataCommand
        ??= new LambdaCommandAsync(OnLoadDataCommandExecuted, CanLoadDataCommandExecute);

    /// <summary> Проверка возможности выполнения - Команда для загрузки данных из репозитория </summary>
    private bool CanLoadDataCommandExecute() => true;

    /// <summary> Логика выполнения - Команда для загрузки данных из репозитория </summary>
    private async Task OnLoadDataCommandExecuted()
    {
        await LoadSeed();
        LoadListCulture();
        LoadListSort();
        LoadListProducer();
    }
    #endregion

    #region Command SeedsChoiceClassCommand - Команда для выбора растений по классам

    /// <summary> Команда для выбора растений по классам </summary>
    private ICommand _SeedsChoiceClassCommand;

    /// <summary> Команда для выбора растений по классам </summary>
    public ICommand SeedsChoiceClassCommand => _SeedsChoiceClassCommand
        ??= new LambdaCommandAsync(OnSeedsChoiceClassCommandExecuted, CanSeedsChoiceClassCommandExecute);

    /// <summary> Проверка возможности выполнения - Команда для выбора растений по классам </summary>
    private bool CanSeedsChoiceClassCommandExecute(object p) => true;

    /// <summary> Логика выполнения - Команда для выбора растений по классам </summary>
    private async Task OnSeedsChoiceClassCommandExecuted(object p)
    {

        var seedsQuery = p.ToString() != "Выбрать все"
                ? _seedsService.Seeds

                .Where(seeds => seeds.Plant.PlantCulture.Class == p.ToString())
                .Select(seeds => new SeedsFromViewModel
                {
                    Id = seeds.Id,
                    Culture = seeds.Plant.PlantCulture.Name,
                    Sort = seeds.Plant.PlantSort.Name,
                    Producer = seeds.Plant.PlantSort.Producer.Name,
                    ExpirationDate = seeds.SeedsInfo.ExpirationDate,
                    QuantityPack = seeds.SeedsInfo.QuantityPack,
                    WeightPack = seeds.SeedsInfo.WeightPack,
                    AmountSeedsQuantity = seeds.SeedsInfo.AmountSeeds,
                    AmountSeedsWeight = seeds.SeedsInfo.AmountSeedsWeight
                })

                : _seedsService.Seeds
                .Select(seeds => new SeedsFromViewModel
                {
                    Id = seeds.Id,
                    Culture = seeds.Plant.PlantCulture.Name,
                    Sort = seeds.Plant.PlantSort.Name,
                    Producer = seeds.Plant.PlantSort.Producer.Name,
                    ExpirationDate = seeds.SeedsInfo.ExpirationDate,
                    QuantityPack = seeds.SeedsInfo.QuantityPack,
                    WeightPack = seeds.SeedsInfo.WeightPack,
                    AmountSeedsQuantity = seeds.SeedsInfo.AmountSeeds,
                    AmountSeedsWeight = seeds.SeedsInfo.AmountSeedsWeight
                })

            ;
        _SeedsView.Source = await seedsQuery.ToArrayAsync();
        //SeedsFromViewModels.AddClear(await seedsQuery.ToArrayAsync());
        OnPropertyChanged(nameof(SeedsView));
    }

    #endregion


    #region Command AddOrCorrectSeedCommand - Команда для создания или редактирования семян

    /// <summary> Команда для создания или редактирования семян </summary>
    private ICommand _AddOrCorrectSeedCommand;

    /// <summary> Команда для создания или редактирования семян </summary>
    public ICommand AddOrCorrectSeedCommand => _AddOrCorrectSeedCommand
        ??= new LambdaCommandAsync(OnAddOrCorrectSeedCommandExecuted, CanAddOrCorrectSeedCommandExecute);

    /// <summary> Проверка возможности выполнения - Команда для создания или редактирования семян </summary>
    private bool CanAddOrCorrectSeedCommandExecute() => true;

    /// <summary> Логика выполнения - Команда для создания или редактирования семян </summary>
    private async Task OnAddOrCorrectSeedCommandExecuted()
    {
        var plant = GetOrCreatePlant();
        var seedsInfo = new SeedsInfo();
        _seedsService.MakeASeed(plant, seedsInfo);
    }
    #endregion


    #endregion


}