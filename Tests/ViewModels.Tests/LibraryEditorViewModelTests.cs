namespace ViewModels.Tests;

public class LibraryEditorViewModelTests
{
    private readonly ISeedsService _seedsService = Substitute.For<ISeedsService>();
    private readonly IUserDialog _userDialog = Substitute.For<IUserDialog>();

    private LibraryEditorViewModel CreateVm(
        ObservableCollection<SortFromSeedsViewModel>? sorts = null,
        ObservableCollection<CultureFromViewModel>? cultures = null,
        ObservableCollection<ProducerFromViewModel>? producers = null,
        ObservableCollection<Seed>? seeds = null)
    {
        return new LibraryEditorViewModel(
            _seedsService,
            _userDialog,
            sorts ?? new ObservableCollection<SortFromSeedsViewModel>(),
            cultures ?? new ObservableCollection<CultureFromViewModel>(),
            producers ?? new ObservableCollection<ProducerFromViewModel>(),
            seeds ?? new ObservableCollection<Seed>());
    }

    // ── SelectedSort copies name to TempName ──────────────────────────────────

    [Fact]
    public void SelectedSort_Set_CopiesNameToTempName()
    {
        var vm = CreateVm();
        var sort = new SortFromSeedsViewModel { Id = 1, Name = "Черри" };

        vm.SelectedSort = sort;

        Assert.Equal("Черри", vm.TempName);
    }

    [Fact]
    public void SelectedSort_NullName_SetsNullTempName()
    {
        var vm = CreateVm();
        var sort = new SortFromSeedsViewModel { Id = 1, Name = null };

        vm.SelectedSort = sort;

        Assert.Null(vm.TempName);
    }

    // ── SelectedCulture copies name to TempName ───────────────────────────────

    [Fact]
    public void SelectedCulture_Set_CopiesNameToTempName()
    {
        var vm = CreateVm();
        var culture = new CultureFromViewModel { Id = 2, Name = "Томат" };

        vm.SelectedCulture = culture;

        Assert.Equal("Томат", vm.TempName);
    }

    // ── SelectedProducer copies name to TempName ──────────────────────────────

    [Fact]
    public void SelectedProducer_Set_CopiesNameToTempName()
    {
        var vm = CreateVm();
        var producer = new ProducerFromViewModel { Id = 3, Name = "Гавриш" };

        vm.SelectedProducer = producer;

        Assert.Equal("Гавриш", vm.TempName);
    }

    // ── TempName overwritten by last selection ────────────────────────────────

    [Fact]
    public void TempName_OverwrittenBySubsequentSelection()
    {
        var vm = CreateVm();
        vm.SelectedSort = new SortFromSeedsViewModel { Name = "Первый" };
        vm.SelectedCulture = new CultureFromViewModel { Name = "Томат" };

        Assert.Equal("Томат", vm.TempName);
    }

    // ── UpdateSortNameCommand always CanExecute ───────────────────────────────

    [Fact]
    public void UpdateSortNameCommand_AlwaysCanExecute()
    {
        var vm = CreateVm();
        Assert.True(vm.UpdateSortNameCommand.CanExecute(null));
    }

    [Fact]
    public void UpdateCultureNameCommand_AlwaysCanExecute()
    {
        var vm = CreateVm();
        Assert.True(vm.UpdateCultureNameCommand.CanExecute(null));
    }

    [Fact]
    public void UpdateProducerNameCommand_AlwaysCanExecute()
    {
        var vm = CreateVm();
        Assert.True(vm.UpdateProducerNameCommand.CanExecute(null));
    }

    // ── Seeds collection is passed through constructor ────────────────────────

    [Fact]
    public void Seeds_InitializedFromConstructor()
    {
        var seed = new Seed { Id = 1, Plant = new Plant
        {
            PlantCulture = new PlantCulture { Name = "Томат" },
            PlantSort = new PlantSort { Name = "Черри", Producer = new Producer { Name = "Г" } }
        }, SeedsInfo = new SeedsInfo() };
        var seeds = new ObservableCollection<Seed> { seed };

        var vm = CreateVm(seeds: seeds);

        Assert.Single(vm.Seeds);
        Assert.Equal(1, vm.Seeds.First().Id);
    }

    [Fact]
    public void Sort_InitializedFromConstructor()
    {
        var sorts = new ObservableCollection<SortFromSeedsViewModel>
        {
            new() { Id = 1, Name = "Черри" },
            new() { Id = 2, Name = "Буян" }
        };
        var vm = CreateVm(sorts: sorts);
        Assert.Equal(2, vm.Sort.Count);
    }
}
