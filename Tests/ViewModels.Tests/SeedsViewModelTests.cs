namespace ViewModels.Tests;

public class SeedsViewModelTests
{
    private readonly ISeedsService _seedsService = Substitute.For<ISeedsService>();
    private readonly IUserDialog _userDialog = Substitute.For<IUserDialog>();
    private readonly IReportService _reportService = Substitute.For<IReportService>();

    private SeedsViewModel CreateVm()
    {
        _seedsService.GetAllSeedsAsync().Returns(new List<Seed>());
        return new SeedsViewModel(_seedsService, _userDialog, _reportService);
    }

    // ── Verification via AddOrCorrectSeedCommand.CanExecute ───────────────────

    [Fact]
    public void AddOrCorrectSeedCommand_AllFieldsFilled_CanExecute()
    {
        var vm = CreateVm();
        SetAllVerificationFields(vm);

        Assert.True(vm.AddOrCorrectSeedCommand.CanExecute(null));
    }

    [Fact]
    public void AddOrCorrectSeedCommand_EmptyQuantityInPac_CannotExecute()
    {
        var vm = CreateVm();
        SetAllVerificationFields(vm);
        vm.AddQuantityInPac = string.Empty;

        Assert.False(vm.AddOrCorrectSeedCommand.CanExecute(null));
    }

    [Fact]
    public void AddOrCorrectSeedCommand_EmptyCulture_CannotExecute()
    {
        var vm = CreateVm();
        SetAllVerificationFields(vm);
        vm.AddCulture = string.Empty;

        Assert.False(vm.AddOrCorrectSeedCommand.CanExecute(null));
    }

    [Fact]
    public void AddOrCorrectSeedCommand_EmptySort_CannotExecute()
    {
        var vm = CreateVm();
        SetAllVerificationFields(vm);
        vm.AddSort = string.Empty;

        Assert.False(vm.AddOrCorrectSeedCommand.CanExecute(null));
    }

    [Fact]
    public void AddOrCorrectSeedCommand_EmptyProducer_CannotExecute()
    {
        var vm = CreateVm();
        SetAllVerificationFields(vm);
        vm.AddProducer = string.Empty;

        Assert.False(vm.AddOrCorrectSeedCommand.CanExecute(null));
    }

    [Fact]
    public void AddOrCorrectSeedCommand_EmptyCostPack_CannotExecute()
    {
        var vm = CreateVm();
        SetAllVerificationFields(vm);
        vm.AddCostPack = string.Empty;

        Assert.False(vm.AddOrCorrectSeedCommand.CanExecute(null));
    }

    [Fact]
    public void AddOrCorrectSeedCommand_EmptyClass_CannotExecute()
    {
        var vm = CreateVm();
        SetAllVerificationFields(vm);
        vm.AddClass = string.Empty;

        Assert.False(vm.AddOrCorrectSeedCommand.CanExecute(null));
    }

    [Fact]
    public void AddOrCorrectSeedCommand_EmptySeedSource_CannotExecute()
    {
        var vm = CreateVm();
        SetAllVerificationFields(vm);
        vm.SeedSource = string.Empty;

        Assert.False(vm.AddOrCorrectSeedCommand.CanExecute(null));
    }

    [Fact]
    public void AddOrCorrectSeedCommand_DefaultDate_CannotExecute()
    {
        var vm = CreateVm();
        SetAllVerificationFields(vm);
        vm.AddBestBy = default;

        Assert.False(vm.AddOrCorrectSeedCommand.CanExecute(null));
    }

    // ── DeleteSeedCommand ─────────────────────────────────────────────────────

    [Fact]
    public void DeleteSeedCommand_NoSelectedItem_CannotExecute()
    {
        var vm = CreateVm();
        vm.SelectedItem = null;

        Assert.False(vm.DeleteSeedCommand.CanExecute(null));
    }

    [Fact]
    public void DeleteSeedCommand_WithSelectedItem_CanExecute()
    {
        var vm = CreateVm();
        vm.SelectedItem = new Seed
        {
            Id = 1,
            Plant = new Plant
            {
                PlantCulture = new PlantCulture { Name = "Томат" },
                PlantSort = new PlantSort { Name = "Черри", Producer = new Producer { Name = "Г" } }
            },
            SeedsInfo = new SeedsInfo()
        };

        Assert.True(vm.DeleteSeedCommand.CanExecute(null));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SetAllVerificationFields(SeedsViewModel vm)
    {
        vm.SeedSource = "Куплено";    // must be first: its setter resets AddProducer
        vm.AddBestBy = new DateTime(DateTime.Now.Year + 1, 12, 31);
        vm.AddClass = "Овощ";
        vm.AddCostPack = "100";
        vm.AddCulture = "Томат";
        vm.AddProducer = "Гавриш";    // must come after SeedSource
        vm.AddQuantityInPac = "20";
        vm.AddSort = "Черри";
    }
}
