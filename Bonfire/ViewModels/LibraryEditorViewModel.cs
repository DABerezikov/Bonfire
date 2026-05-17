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

public class LibraryEditorViewModel(ISeedsService seedsService, IUserDialog userDialog, SeedsViewModel seedsViewModel)
    : ViewModel
{
    private readonly ISeedsService _SeedsService = seedsService;
    private readonly IUserDialog _UserDialog = userDialog;
    private readonly SeedsViewModel _SeedsViewModel = seedsViewModel;

    public ObservableCollection<SortFromSeedsViewModel> Sort => _SeedsViewModel.AddSortList;
    public ObservableCollection<CultureFromViewModel> Culture => _SeedsViewModel.AddCultureList;
    public ObservableCollection<ProducerFromViewModel> Producer => _SeedsViewModel.AddProducerList;
    public ObservableCollection<Seed> Seeds => _SeedsViewModel.Seeds;

    private string? _TempName;
    public string? TempName
    {
        get => _TempName;
        set => Set(ref _TempName, value);
    }

    private SortFromSeedsViewModel _SelectedSort;
    public SortFromSeedsViewModel SelectedSort
    {
        get => _SelectedSort;
        set { Set(ref _SelectedSort, value); TempName = value?.Name; }
    }

    private CultureFromViewModel _SelectedCulture;
    public CultureFromViewModel SelectedCulture
    {
        get => _SelectedCulture;
        set { Set(ref _SelectedCulture, value); TempName = value?.Name; }
    }

    private ProducerFromViewModel _SelectedProducer;
    public ProducerFromViewModel SelectedProducer
    {
        get => _SelectedProducer;
        set { Set(ref _SelectedProducer, value); TempName = value?.Name; }
    }

    // Команды

    private ICommand _UpdateSortNameCommand;
    public ICommand UpdateSortNameCommand => _UpdateSortNameCommand
        ??= new LambdaCommandAsync(async () =>
        {
            var sort = Seeds.First(s => s.Plant.PlantSort.Id == SelectedSort.Id).Plant.PlantSort;
            sort.Name = SelectedSort.Name;
            await _SeedsService.UpdateSort(sort);
        }, () => SelectedSort != null);

    private ICommand _UpdateCultureNameCommand;
    public ICommand UpdateCultureNameCommand => _UpdateCultureNameCommand
        ??= new LambdaCommandAsync(async () =>
        {
            var culture = Seeds.First(s => s.Plant.PlantCulture.Id == SelectedCulture.Id).Plant.PlantCulture;
            culture.Name = SelectedCulture.Name;
            await _SeedsService.UpdateCulture(culture);
        }, () => SelectedCulture != null);

    private ICommand _UpdateProducerNameCommand;
    public ICommand UpdateProducerNameCommand => _UpdateProducerNameCommand
        ??= new LambdaCommandAsync(async () =>
        {
            var producer = Seeds.First(s => s.Plant.PlantSort.Producer.Id == SelectedProducer.Id).Plant.PlantSort.Producer;
            producer.Name = SelectedProducer.Name;
            await _SeedsService.UpdateProducer(producer);
        }, () => SelectedProducer != null);
}
