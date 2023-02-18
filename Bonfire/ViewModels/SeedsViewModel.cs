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

public class SeedsViewModel : ViewModel
{
    private readonly ISeedsService _seedsService;
    private readonly IUserDialog _userDialog;

    
    
    public SeedsViewModel(ISeedsService seedsService, IUserDialog userDialog)
    {
        _seedsService = seedsService;
        _userDialog = userDialog;
        _SeedsViewSource = new CollectionViewSource
        {
            SortDescriptions =
            {
                new SortDescription(nameof(SeedsFromViewModel.Culture), ListSortDirection.Ascending)

            }

        };
        _SeedsViewSource.Filter += _SeedsViewSource_Filter;
    }

    

    #region FilterSeeds - Фильтрация по культуре

    public ICollectionView SeedsView => _SeedsViewSource.View;
    private readonly CollectionViewSource _SeedsViewSource;

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
                _SeedsViewSource.View.Refresh();
            }
        } 
    }

    #endregion
    private void _SeedsViewSource_Filter(object sender, FilterEventArgs e)
    {
        if (!(e.Item is SeedsFromViewModel seed) || string.IsNullOrEmpty(SeedFilter)) return;
        if (!seed.Culture.Contains(SeedFilter))
            e.Accepted = false;
    }

    #endregion

    #region Seeds : ObservableCollection<SeedsFromViewModel> - Коллекция семян

    /// <summary>Коллекция семян</summary>
    private ObservableCollection<SeedsFromViewModel> _SeedsFromViewModels = new ();

    /// <summary>Коллекция семян</summary>
    public ObservableCollection<SeedsFromViewModel> SeedsFromViewModels
    {
        get => _SeedsFromViewModels;
        set
        {
            if (Set(ref _SeedsFromViewModels, value))
            {
                _SeedsViewSource.Source = value;
                OnPropertyChanged(nameof(SeedsView));
            };
        }
    }
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
      var seedsQuery  = _seedsService.Seeds
            .Select(seeds=>new SeedsFromViewModel
            {
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
    }

    #endregion

    #region Command SeedsChoiceCommand - Команда для выбора фруктов

    /// <summary> Команда для выбора фруктов </summary>
    private ICommand _SeedsChoiceCommand;

    /// <summary> Команда для выбора фруктов </summary>
    public ICommand SeedsChoiceCommand => _SeedsChoiceCommand
        ??= new LambdaCommandAsync(OnSeedsChoiceCommandExecuted, CanSeedsChoiceCommandExecute);

    /// <summary> Проверка возможности выполнения - Команда для выбора фруктов </summary>
    private bool CanSeedsChoiceCommandExecute(object p) => true;

    /// <summary> Логика выполнения - Команда для выбора фруктов </summary>
    private async Task OnSeedsChoiceCommandExecuted(object p)
    {
        
        var seedsQuery = p.ToString()!="Выбрать все"
                ? _seedsService.Seeds
                
                .Where(seeds => seeds.Plant.PlantCulture.Name == p.ToString())
                .Select(seeds => new SeedsFromViewModel
                {
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
    }

    #endregion
}