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

    #region AddProducer : string - Выбор поставщика семян

    /// <summary>Выбор поставщика семян</summary>
    private string _AddProducer;

    /// <summary>Выбор поставщика семян</summary>
    public string AddProducer
    {
        get => _AddProducer;
        set => Set(ref _AddProducer, value);
    }

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

    #region Выбор культуры для добавления семян

    public ICollectionView CultureListView  => _CultureListView?.View;
    private CollectionViewSource _CultureListView;
    

    private void _CultureListView_Filter(object sender, FilterEventArgs e)
    {
        if (!(e.Item is CultureFromViewModel culture) || string.IsNullOrEmpty(AddCulture)) return;
        if (culture.Name != null && !culture.Name.Contains(AddCulture, StringComparison.OrdinalIgnoreCase))
            e.Accepted = false;
    }

    #region AddCultureList : List<string> - Список культур для добавления семян

    /// <summary>Список культур для добавления семян</summary>
    private List<CultureFromViewModel> _AddCultureList = new();

    /// <summary>Список культур для добавления семян</summary>
    public List<CultureFromViewModel> AddCultureList
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
            .OrderBy(s => s);
        var addListCulture = _seedsService.Seeds
            .Select(seeds => new CultureFromViewModel
            {
                Id = seeds.Plant.PlantCulture.Id,
                Name = seeds.Plant.PlantCulture.Name
            })
            .OrderBy(s=>s.Name);
        ListCulture.AddRange(listCultureQuery.ToListAsync().Result.ToHashSet());
        AddCultureList.AddRange(addListCulture.ToListAsync().Result.ToHashSet());
        _CultureListView.Source = AddCultureList;
        
        
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


    #endregion


}