namespace ViewModels.Tests;

public class LibraryEditorViewModelTests
{
    private readonly ISeedsService _seedsService = Substitute.For<ISeedsService>();
    private readonly IUserDialog _userDialog = Substitute.For<IUserDialog>();
    private readonly IReportService _reportService = Substitute.For<IReportService>();

    private SeedsViewModel CreateSeedsVm(
        ObservableCollection<SortFromSeedsViewModel>? sorts = null,
        ObservableCollection<CultureFromViewModel>? cultures = null,
        ObservableCollection<ProducerFromViewModel>? producers = null,
        ObservableCollection<Seed>? seeds = null)
    {
        _seedsService.Seeds.Returns(Enumerable.Empty<Seed>().AsQueryable());
        var vm = new SeedsViewModel(_seedsService, _userDialog, _reportService);
        if (sorts != null) vm.AddSortList = sorts;
        if (cultures != null) vm.AddCultureList = cultures;
        if (producers != null) vm.AddProducerList = producers;
        if (seeds != null) vm.Seeds = seeds;
        return vm;
    }

    private LibraryEditorViewModel CreateVm(
        ObservableCollection<SortFromSeedsViewModel>? sorts = null,
        ObservableCollection<CultureFromViewModel>? cultures = null,
        ObservableCollection<ProducerFromViewModel>? producers = null,
        ObservableCollection<Seed>? seeds = null)
    {
        var seedsVm = CreateSeedsVm(sorts, cultures, producers, seeds);
        return new LibraryEditorViewModel(_seedsService, _userDialog, seedsVm);
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

    // ── UpdateXxxNameCommand CanExecute depends on selection ──────────────────

    [Fact]
    public void UpdateSortNameCommand_NoSelectedSort_CannotExecute()
    {
        var vm = CreateVm();
        Assert.False(vm.UpdateSortNameCommand.CanExecute(null));
    }

    [Fact]
    public void UpdateSortNameCommand_WithSelectedSort_CanExecute()
    {
        var vm = CreateVm();
        vm.SelectedSort = new SortFromSeedsViewModel { Id = 1, Name = "Черри" };
        Assert.True(vm.UpdateSortNameCommand.CanExecute(null));
    }

    [Fact]
    public void UpdateCultureNameCommand_NoSelectedCulture_CannotExecute()
    {
        var vm = CreateVm();
        Assert.False(vm.UpdateCultureNameCommand.CanExecute(null));
    }

    [Fact]
    public void UpdateCultureNameCommand_WithSelectedCulture_CanExecute()
    {
        var vm = CreateVm();
        vm.SelectedCulture = new CultureFromViewModel { Id = 1, Name = "Томат" };
        Assert.True(vm.UpdateCultureNameCommand.CanExecute(null));
    }

    [Fact]
    public void UpdateProducerNameCommand_NoSelectedProducer_CannotExecute()
    {
        var vm = CreateVm();
        Assert.False(vm.UpdateProducerNameCommand.CanExecute(null));
    }

    [Fact]
    public void UpdateProducerNameCommand_WithSelectedProducer_CanExecute()
    {
        var vm = CreateVm();
        vm.SelectedProducer = new ProducerFromViewModel { Id = 1, Name = "Гавриш" };
        Assert.True(vm.UpdateProducerNameCommand.CanExecute(null));
    }

    // ── Seeds collection is passed through SeedsViewModel ────────────────────

    [Fact]
    public void Seeds_InitializedFromSeedsViewModel()
    {
        var seed = new Seed
        {
            Id = 1,
            Plant = new Plant
            {
                PlantCulture = new PlantCulture { Name = "Томат" },
                PlantSort = new PlantSort { Name = "Черри", Producer = new Producer { Name = "Г" } }
            },
            SeedsInfo = new SeedsInfo()
        };
        var seeds = new ObservableCollection<Seed> { seed };

        var vm = CreateVm(seeds: seeds);

        Assert.Single(vm.Seeds);
        Assert.Equal(1, vm.Seeds.First().Id);
    }

    [Fact]
    public void Sort_InitializedFromSeedsViewModel()
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
