using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Bonfire.Infrastructure.Commands;
using Bonfire.Models;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels.Base;
using BonfireDB.Entities;

namespace Bonfire.ViewModels;

public class LibraryEditorViewModel : ViewModel
{
    

    private readonly ISeedsService _SeedsService;
    private readonly IUserDialog _UserDialog;
   

    public LibraryEditorViewModel ( ISeedsService seedsService,
                                    IUserDialog userDialog,
                                    ObservableCollection<SortFromViewModel> sort,
                                    ObservableCollection<CultureFromViewModel> culture,
                                    ObservableCollection<ProducerFromViewModel> producer,
                                    ObservableCollection<Seed> seeds)
    {
        Seeds = seeds;

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

    #region TempName : string - Временная переменная для редактирования

    /// <summary>Временная переменная для редактирования</summary>
    private string? _TempName;

    /// <summary>Временная переменная для редактирования</summary>
    public string? TempName
    {
        get => _TempName;
        set => Set(ref _TempName, value);
    }

    #endregion

    #region SelectedSort : SortFromViewModel - Выбранный сорт

    /// <summary>Выбранный сорт</summary>
    private SortFromViewModel _SelectedSort;

    /// <summary>Выбранный сорт</summary>
    public SortFromViewModel SelectedSort
    {
        get => _SelectedSort;
        set
        {
            Set(ref _SelectedSort, value);
            TempName = _SelectedSort.Name;
        }
    }

    #endregion

    #region SelectedCulture : CultureFromViewModel - Выбранная культура

    /// <summary>Выбранная культура</summary>
    private CultureFromViewModel _SelectedCulture;

    /// <summary>Выбранная культура</summary>
    public CultureFromViewModel SelectedCulture
    {
        get => _SelectedCulture;
        set
        {
            Set(ref _SelectedCulture, value);
            TempName = _SelectedCulture.Name;
        }
    }

    #endregion
    
    #region SelectedProducer : ProducerFromViewModel - Выбранный производитель

    /// <summary>Выбранный производитель</summary>
    private ProducerFromViewModel _SelectedProducer;

    /// <summary>Выбранный производитель</summary>
    public ProducerFromViewModel SelectedProducer
    {
        get => _SelectedProducer;
        set
        {
            Set(ref _SelectedProducer, value);
            TempName = _SelectedProducer.Name;
        }
    }

    #endregion

    #region Command UpdateSortNameCommand - Команда для обновления имени сорта

    /// <summary> Команда для обновления имени сорта </summary>
    private ICommand _UpdateSortNameCommand;

    /// <summary> Команда для обновления имени сорта </summary>
    public ICommand UpdateSortNameCommand => _UpdateSortNameCommand
        ??= new LambdaCommandAsync(OnUpdateSortNameCommandExecuted, CanUpdateSortNameCommandExecute);

    /// <summary> Проверка возможности выполнения - Команда для обновления имени сорта </summary>
    private bool CanUpdateSortNameCommandExecute() => true;

    /// <summary> Логика выполнения - Команда для обновления имени сорта </summary>
    private async Task OnUpdateSortNameCommandExecuted()
    {
        var tempSort= Seeds.First(s => s.Plant.PlantSort.Id == SelectedSort.Id).Plant.PlantSort;
        tempSort.Name = SelectedSort.Name;
        await _SeedsService.UpdateSort(tempSort);
        
    }
    #endregion
    
    #region Command UpdateCultureNameCommand - Команда для обновления имени сорта

    /// <summary> Команда для обновления имени сорта </summary>
    private ICommand _UpdateCultureNameCommand;

    /// <summary> Команда для обновления имени сорта </summary>
    public ICommand UpdateCultureNameCommand => _UpdateCultureNameCommand
        ??= new LambdaCommandAsync(OnUpdateCultureNameCommandExecuted, CanUpdateCultureNameCommandExecute);

    /// <summary> Проверка возможности выполнения - Команда для обновления имени сорта </summary>
    private bool CanUpdateCultureNameCommandExecute() => true;

    /// <summary> Логика выполнения - Команда для обновления имени сорта </summary>
    private async Task OnUpdateCultureNameCommandExecuted()
    {
        var tempCulture= Seeds.First(s => s.Plant.PlantCulture.Id == SelectedCulture.Id).Plant.PlantCulture;
        tempCulture.Name = SelectedCulture.Name;
        await _SeedsService.UpdateCulture(tempCulture);
        
    }
    #endregion
    
    #region Command UpdateProducerNameCommand - Команда для обновления имени сорта

    /// <summary> Команда для обновления имени сорта </summary>
    private ICommand _UpdateProducerNameCommand;

    /// <summary> Команда для обновления имени сорта </summary>
    public ICommand UpdateProducerNameCommand => _UpdateProducerNameCommand
        ??= new LambdaCommandAsync(OnUpdateProducerNameCommandExecuted, CanUpdateProducerNameCommandExecute);

    /// <summary> Проверка возможности выполнения - Команда для обновления имени сорта </summary>
    private bool CanUpdateProducerNameCommandExecute() => true;

    /// <summary> Логика выполнения - Команда для обновления имени сорта </summary>
    private async Task OnUpdateProducerNameCommandExecuted()
    {
        var tempProducer= Seeds.First(s => s.Plant.PlantSort.Producer.Id == SelectedProducer.Id).Plant.PlantSort.Producer;
        tempProducer.Name = SelectedProducer.Name;
        await _SeedsService.UpdateProducer(tempProducer);
        
    }
    #endregion


}