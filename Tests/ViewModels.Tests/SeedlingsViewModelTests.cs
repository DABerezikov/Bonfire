using MoonCalendar;

namespace ViewModels.Tests;

public class SeedlingsViewModelTests
{
    private readonly ISeedlingsService _seedlingsService = Substitute.For<ISeedlingsService>();
    private readonly ISeedsService _seedsService = Substitute.For<ISeedsService>();
    private readonly IUserDialog _userDialog = Substitute.For<IUserDialog>();
    private readonly IReportService _reportService = Substitute.For<IReportService>();

    private SeedlingsViewModel CreateVm()
    {
        _seedsService.GetAllSeedsAsync().Returns(new List<Seed>());
        _seedlingsService.GetAllSeedlingsAsync().Returns(new List<Seedling>());
        _seedlingsService.Lunar.Returns(new MoonPhase());
        return new SeedlingsViewModel(_seedlingsService, _seedsService, _userDialog, _reportService);
    }

    // ── Verification via AddOrCorrectSeedlingCommand.CanExecute ───────────────

    [Fact]
    public void AddOrCorrectSeedlingCommand_AllFieldsFilled_CanExecute()
    {
        var vm = CreateVm();
        SetAllVerificationFields(vm);

        Assert.True(vm.AddOrCorrectSeedlingCommand.CanExecute(null));
    }

    [Fact]
    public void AddOrCorrectSeedlingCommand_DefaultPlantingDate_CannotExecute()
    {
        var vm = CreateVm();
        SetAllVerificationFields(vm);
        vm.PlantingDate = default;

        Assert.False(vm.AddOrCorrectSeedlingCommand.CanExecute(null));
    }

    [Fact]
    public void AddOrCorrectSeedlingCommand_EmptySize_CannotExecute()
    {
        var vm = CreateVm();
        SetAllVerificationFields(vm);
        vm.AddSize = string.Empty;

        Assert.False(vm.AddOrCorrectSeedlingCommand.CanExecute(null));
    }

    [Fact]
    public void AddOrCorrectSeedlingCommand_EmptyCulture_CannotExecute()
    {
        var vm = CreateVm();
        SetAllVerificationFields(vm);
        vm.AddCulture = string.Empty;

        Assert.False(vm.AddOrCorrectSeedlingCommand.CanExecute(null));
    }

    [Fact]
    public void AddOrCorrectSeedlingCommand_EmptyProducer_CannotExecute()
    {
        var vm = CreateVm();
        SetAllVerificationFields(vm);
        vm.AddProducer = string.Empty;

        Assert.False(vm.AddOrCorrectSeedlingCommand.CanExecute(null));
    }

    [Fact]
    public void AddOrCorrectSeedlingCommand_ZeroQuantity_CannotExecute()
    {
        var vm = CreateVm();
        SetAllVerificationFields(vm);
        vm.AddQuantity = 0.0;

        Assert.False(vm.AddOrCorrectSeedlingCommand.CanExecute(null));
    }

    [Fact]
    public void AddOrCorrectSeedlingCommand_EmptySort_CannotExecute()
    {
        var vm = CreateVm();
        SetAllVerificationFields(vm);
        vm.AddSort = string.Empty;

        Assert.False(vm.AddOrCorrectSeedlingCommand.CanExecute(null));
    }

    [Fact]
    public void AddOrCorrectSeedlingCommand_EmptySeedlingSource_CannotExecute()
    {
        var vm = CreateVm();
        SetAllVerificationFields(vm);
        vm.SeedlingSource = string.Empty;

        Assert.False(vm.AddOrCorrectSeedlingCommand.CanExecute(null));
    }

    // ── AddQuantity clamping ──────────────────────────────────────────────────

    [Fact]
    public void AddQuantity_ExceedsPlantable_ClampedToPlantable()
    {
        var vm = CreateVm();
        vm.Plantable = 10.0;

        vm.AddQuantity = 50.0;

        Assert.Equal(10.0, vm.AddQuantity);
    }

    [Fact]
    public void AddQuantity_WithinPlantable_SetToValue()
    {
        var vm = CreateVm();
        vm.Plantable = 100.0;

        vm.AddQuantity = 30.0;

        Assert.Equal(30.0, vm.AddQuantity);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SetAllVerificationFields(SeedlingsViewModel vm)
    {
        vm.PlantingDate = DateTime.Today;
        vm.AddCulture = "Томат";    // must be first: its setter resets AddSize, AddSort, AddProducer, AddQuantity
        vm.AddSize = "шт.";
        vm.AddProducer = "Гавриш";
        vm.Plantable = 100.0;
        vm.AddQuantity = 5.0;
        vm.AddSort = "Черри";
        vm.SeedlingSource = "Посеяно";
    }
}
