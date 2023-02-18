using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;
using BonfireDB.Entities;


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
                new SortDescription(nameof(Seed.Plant.PlantCulture.Name), ListSortDirection.Ascending)

            }

        };
        _SeedsViewSource.Filter += _SeedsViewSource_Filter;
    }

    

    #region FilterSeeds - Фильтрация по культуре

    public ICollectionView SeedsView => _SeedsViewSource.View;
    private readonly CollectionViewSource _SeedsViewSource;

    #region SeedFilter : string - Искомое слово

    /// <summary>Искомое слово</summary>
    private string _SeedFilter;

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
        if (!(e.Item is Seed seed) || string.IsNullOrEmpty(SeedFilter)) return;
        if (!seed.Plant.PlantCulture.Name.Contains(SeedFilter))
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
        set
        {
            if (Set(ref _Seeds, value))
            {
                _SeedsViewSource.Source = value;
                OnPropertyChanged(nameof(SeedsView));
            };
        }
    }
    #endregion

}