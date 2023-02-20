using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models;
using Bonfire.Services.Extensions;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;


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
    }

    

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

    #region Seeds : ObservableCollection<SeedsFromViewModel> - Коллекция семян

    /// <summary>Коллекция семян</summary>
    private ObservableCollection<SeedsFromViewModel> _SeedsFromViewModels = new();

    /// <summary>Коллекция семян</summary>
    public ObservableCollection<SeedsFromViewModel> SeedsFromViewModels
    {
        get => _SeedsFromViewModels;
        set => Set(ref _SeedsFromViewModels, value);
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

    #region ListCulture : IEnumerable - Список культур

    /// <summary>Список культур</summary>
    private List<string> _ListCulture = new List<string> { "Выбрать все" };

    /// <summary>Список культур</summary>
    public List<string> ListCulture
    {
        get => _ListCulture;
        set => Set(ref _ListCulture, value);
    }

    #endregion

    #region Методы

    #region Метод обновления представления

    private void RefreshSeedsView()
    {
        _SeedsView.Source = SeedsFromViewModels;
        OnPropertyChanged(nameof(SeedsView));
    }

    #endregion

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
        SeedsFromViewModels.AddClear(await seedsQuery.ToArrayAsync());
        RefreshSeedsView();
    }

    #endregion

    #region Метод загрузки списка культур

    private void LoadListCulture()
    {
        var listCultureQuery = _seedsService.Seeds
            .Select(seeds => seeds.Plant.PlantCulture.Name)
            .OrderBy(s => s);
        ListCulture.AddRange(listCultureQuery.ToListAsync().Result.ToHashSet());
    }

    #endregion

    #endregion


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
        
        var seedsQuery = p.ToString()!="Выбрать все"
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
        SeedsFromViewModels.AddClear(await seedsQuery.ToArrayAsync());
        RefreshSeedsView();
    }

    #endregion
}