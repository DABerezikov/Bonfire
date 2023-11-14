using System.Collections.ObjectModel;
using Bonfire.Models;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;

namespace Bonfire.ViewModels;

public class LibraryEditorViewModel : ViewModel
{
    
    private readonly ISeedsService _SeedsService;
    private readonly IUserDialog _UserDialog;
   

    public LibraryEditorViewModel ( ISeedsService seedsService,
                                    IUserDialog userDialog,
                                    ObservableCollection<SortFromViewModel> sort,
                                    ObservableCollection<CultureFromViewModel> culture,
                                    ObservableCollection<ProducerFromViewModel> producer)
    {
        
        _SeedsService = seedsService;
        _UserDialog = userDialog;
        _Sort = sort;
        _Culture = culture;
        _Producer = producer;
    }

    #region Sort : ObservableCollection<SortFromViewModel> - Коллекция сортов

    /// <summary>Коллекция сортов</summary>
    private ObservableCollection<SortFromViewModel> _Sort;

    /// <summary>Коллекция сортов</summary>
    public ObservableCollection<SortFromViewModel> Sort
    {
        get => _Sort;
        set => Set(ref _Sort, value);
    }

    #endregion

    #region Culture : ObservableCollection<CultureFromViewModel> - Коллекция Культур

    /// <summary>Коллекция Культур</summary>
    private ObservableCollection<CultureFromViewModel> _Culture;

    /// <summary>Коллекция Культур</summary>
    public ObservableCollection<CultureFromViewModel> Culture
    {
        get => _Culture;
        set => Set(ref _Culture, value);
    }

    #endregion

    #region Producer : ObservableCollection<ProducerFromViewModel> - Коллекция Производителей

    /// <summary>Коллекция Производителей</summary>
    private ObservableCollection<ProducerFromViewModel> _Producer;

    /// <summary>Коллекция Производителей</summary>
    public ObservableCollection<ProducerFromViewModel> Producer
    {
        get => _Producer;
        set => Set(ref _Producer, value);
    }

    #endregion

}