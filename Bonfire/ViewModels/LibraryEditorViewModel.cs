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
    private readonly ISeedsService _seedsService = seedsService;
    private readonly IUserDialog _userDialog = userDialog;
    private readonly SeedsViewModel _seedsViewModel = seedsViewModel;

    public ObservableCollection<SortFromSeedsViewModel> Sort => _seedsViewModel.AddSortList;
    public ObservableCollection<CultureFromViewModel> Culture => _seedsViewModel.AddCultureList;
    public ObservableCollection<ProducerFromViewModel> Producer => _seedsViewModel.AddProducerList;
    public ObservableCollection<Seed> Seeds => _seedsViewModel.Seeds;

    public string? TempName
    {
        get;
        set => Set(ref field, value);
    }

    public SortFromSeedsViewModel SelectedSort
    {
        get;
        set
        {
            Set(ref field, value);
            TempName = value?.Name;
        }
    }

    public CultureFromViewModel SelectedCulture
    {
        get;
        set
        {
            Set(ref field, value);
            TempName = value?.Name;
        }
    }

    public ProducerFromViewModel SelectedProducer
    {
        get;
        set
        {
            Set(ref field, value);
            TempName = value?.Name;
        }
    }

    // Команды

    public ICommand UpdateSortNameCommand => field
        ??= new LambdaCommandAsync(async () =>
        {
            var sort = Seeds.First(s => s.Plant.PlantSort.Id == SelectedSort.Id).Plant.PlantSort;
            sort.Name = SelectedSort.Name;
            await _seedsService.UpdateSort(sort);
        }, () => SelectedSort != null);

    public ICommand UpdateCultureNameCommand => field
        ??= new LambdaCommandAsync(async () =>
        {
            var culture = Seeds.First(s => s.Plant.PlantCulture.Id == SelectedCulture.Id).Plant.PlantCulture;
            culture.Name = SelectedCulture.Name;
            await _seedsService.UpdateCulture(culture);
        }, () => SelectedCulture != null);

    public ICommand UpdateProducerNameCommand => field
        ??= new LambdaCommandAsync(async () =>
        {
            var producer = Seeds.First(s => s.Plant.PlantSort.Producer.Id == SelectedProducer.Id).Plant.PlantSort.Producer;
            producer.Name = SelectedProducer.Name;
            await _seedsService.UpdateProducer(producer);
        }, () => SelectedProducer != null);
}
