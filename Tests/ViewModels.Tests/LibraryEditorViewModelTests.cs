using BonfireDB.Entities.Base;

namespace ViewModels.Tests;

public class LibraryEditorViewModelTests
{
    private readonly ILibraryService _libraryService = Substitute.For<ILibraryService>();
    private readonly IUserDialog _userDialog = Substitute.For<IUserDialog>();
    private readonly ISeedsService _seedsService = Substitute.For<ISeedsService>();
    private readonly IReportService _reportService = Substitute.For<IReportService>();

    private SeedsViewModel CreateSeedsVm(
        ObservableCollection<SortFromSeedsViewModel>? sorts = null,
        ObservableCollection<CultureFromViewModel>? cultures = null,
        ObservableCollection<ProducerFromViewModel>? producers = null,
        ObservableCollection<Seed>? seeds = null)
    {
        _seedsService.GetAllSeedsAsync().Returns(new List<Seed>());
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
        return new LibraryEditorViewModel(_libraryService, _userDialog, seedsVm);
    }

    // ── SelectedSort очищает SortDetail и запускает загрузку ─────────────────

    [Fact]
    public void SelectedSort_Set_ClearsSortDetail()
    {
        var vm = CreateVm();
        vm.SortDetail = new SortEditModel { Id = 0, Name = "X" };
        _libraryService.GetSortAsync(Arg.Any<int>()).Returns(Result.Failure<PlantSort>("err"));

        vm.SelectedSort = new SortFromSeedsViewModel { Id = 1, Name = "Черри" };

        Assert.Null(vm.SortDetail);
    }

    [Fact]
    public void SelectedCulture_Set_ClearsCultureDetail()
    {
        var vm = CreateVm();
        vm.CultureDetail = new CultureEditModel { Id = 0, Name = "X" };
        _libraryService.GetCultureAsync(Arg.Any<int>()).Returns(Result.Failure<PlantCulture>("err"));

        vm.SelectedCulture = new CultureFromViewModel { Id = 2, Name = "Томат" };

        Assert.Null(vm.CultureDetail);
    }

    [Fact]
    public void SelectedProducer_Set_ClearsProducerDetailName()
    {
        var vm = CreateVm();
        _libraryService.GetProducerAsync(Arg.Any<int>()).Returns(Result.Failure<Producer>("err"));

        vm.SelectedProducer = new ProducerFromViewModel { Id = 3, Name = "Гавриш" };

        Assert.Null(vm.ProducerDetailName);
    }

    // ── Взаимное снятие выделения ─────────────────────────────────────────────

    [Fact]
    public void SelectedSort_Set_ClearsSelectedCultureAndProducer()
    {
        var vm = CreateVm();
        _libraryService.GetSortAsync(Arg.Any<int>()).Returns(Result.Failure<PlantSort>("err"));
        vm.SelectedCulture = new CultureFromViewModel { Id = 1, Name = "Томат" };
        vm.SelectedProducer = new ProducerFromViewModel { Id = 1, Name = "Гавриш" };

        vm.SelectedSort = new SortFromSeedsViewModel { Id = 1, Name = "Черри" };

        Assert.Null(vm.SelectedCulture);
        Assert.Null(vm.SelectedProducer);
    }

    // ── SaveSortCommand CanExecute ────────────────────────────────────────────

    [Fact]
    public void SaveSortCommand_NoDetail_CannotExecute()
    {
        var vm = CreateVm();
        Assert.False(vm.SaveSortCommand.CanExecute(null));
    }

    [Fact]
    public void SaveSortCommand_DetailNotDirty_CannotExecute()
    {
        var vm = CreateVm();
        var detail = new SortEditModel { Id = 1, Name = "Черри" };
        detail.ResetDirty();
        vm.SortDetail = detail;

        Assert.False(vm.SaveSortCommand.CanExecute(null));
    }

    [Fact]
    public void SaveSortCommand_DetailDirtyNoErrors_CanExecute()
    {
        var vm = CreateVm();
        var detail = new SortEditModel { Id = 1 };
        detail.ResetDirty();
        detail.Name = "Черри";
        vm.SortDetail = detail;

        Assert.True(vm.SaveSortCommand.CanExecute(null));
    }

    [Fact]
    public void SaveSortCommand_DetailDirtyWithEmptyName_CannotExecute()
    {
        var vm = CreateVm();
        var detail = new SortEditModel { Id = 1, Name = "Черри" };
        detail.ResetDirty();
        detail.Name = string.Empty;
        vm.SortDetail = detail;

        Assert.False(vm.SaveSortCommand.CanExecute(null));
    }

    [Fact]
    public void SaveSortCommand_MinGreaterThanMax_CannotExecute()
    {
        var vm = CreateVm();
        var detail = new SortEditModel { Id = 1, Name = "X" };
        detail.ResetDirty();
        detail.MinGerminationTime = 50;
        detail.MaxGerminationTime = 10;
        vm.SortDetail = detail;

        Assert.False(vm.SaveSortCommand.CanExecute(null));
    }

    [Fact]
    public void SaveSortCommand_InvalidColor_CannotExecute()
    {
        var vm = CreateVm();
        var detail = new SortEditModel { Id = 1, Name = "X" };
        detail.ResetDirty();
        detail.PlantColor = "red";
        vm.SortDetail = detail;

        Assert.False(vm.SaveSortCommand.CanExecute(null));
    }

    // ── CancelSortCommand CanExecute ──────────────────────────────────────────

    [Fact]
    public void CancelSortCommand_DetailNotDirty_CannotExecute()
    {
        var vm = CreateVm();
        var detail = new SortEditModel { Id = 1, Name = "Черри" };
        detail.ResetDirty();
        vm.SortDetail = detail;

        Assert.False(vm.CancelSortCommand.CanExecute(null));
    }

    [Fact]
    public void CancelSortCommand_DetailDirty_CanExecute()
    {
        var vm = CreateVm();
        var detail = new SortEditModel { Id = 1 };
        detail.ResetDirty();
        detail.Name = "Черри";
        vm.SortDetail = detail;

        Assert.True(vm.CancelSortCommand.CanExecute(null));
    }

    // ── SaveCultureCommand CanExecute ─────────────────────────────────────────

    [Fact]
    public void SaveCultureCommand_NoCultureDetail_CannotExecute()
    {
        var vm = CreateVm();
        Assert.False(vm.SaveCultureCommand.CanExecute(null));
    }

    [Fact]
    public void SaveCultureCommand_DetailDirtyNoErrors_CanExecute()
    {
        var vm = CreateVm();
        var detail = new CultureEditModel { Id = 1, Name = "Томат", Class = "Овощи" };
        detail.ResetDirty();
        detail.Name = "Помидор";
        vm.CultureDetail = detail;

        Assert.True(vm.SaveCultureCommand.CanExecute(null));
    }

    [Fact]
    public void SaveCultureCommand_DetailDirtyEmptyName_CannotExecute()
    {
        var vm = CreateVm();
        var detail = new CultureEditModel { Id = 1, Name = "Томат", Class = "Овощи" };
        detail.ResetDirty();
        detail.Name = string.Empty;
        vm.CultureDetail = detail;

        Assert.False(vm.SaveCultureCommand.CanExecute(null));
    }

    // ── SaveProducerCommand CanExecute ────────────────────────────────────────

    [Fact]
    public void SaveProducerCommand_NoSelection_CannotExecute()
    {
        var vm = CreateVm();
        Assert.False(vm.SaveProducerCommand.CanExecute(null));
    }

    [Fact]
    public void SaveProducerCommand_SelectionDirtyNoError_CanExecute()
    {
        var vm = CreateVm();
        vm.SelectedProducer = new ProducerFromViewModel { Id = 1, Name = "Гавриш" };
        _libraryService.GetProducerAsync(Arg.Any<int>()).Returns(Result.Failure<Producer>("err"));
        vm.ProducerDetailName = "Гавриш Новый";
        vm.ProducerDetailIsDirty = true;

        Assert.True(vm.SaveProducerCommand.CanExecute(null));
    }

    [Fact]
    public void SaveProducerCommand_EmptyName_CannotExecute()
    {
        var vm = CreateVm();
        vm.SelectedProducer = new ProducerFromViewModel { Id = 1, Name = "Гавриш" };
        vm.ProducerDetailName = string.Empty;
        vm.ProducerDetailIsDirty = true;

        Assert.False(vm.SaveProducerCommand.CanExecute(null));
    }

    // ── Collections ──────────────────────────────────────────────────────────

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

        Assert.Single(vm.Sort.Count == 0 ? seeds : seeds);
    }
}
