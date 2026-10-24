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

    [Fact]
    public void SelectedItem_DirtyPackageAndDiscardRefused_KeepsSelectionAndEdits()
    {
        var vm = CreateVm();
        var first = MakeSeed(1);
        vm.SelectedItem = first;
        vm.EditedItem.SeedsInfo.Note = "Черновик";
        _userDialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        vm.SelectedItem = MakeSeed(2);
        Assert.Same(first, vm.SelectedItem);
        Assert.Equal("Черновик", vm.EditedItem.SeedsInfo.Note);
        Assert.Null(first.SeedsInfo.Note);
    }

    [Fact]
    public void SelectedItem_DirtyPackageAndDiscardConfirmed_DiscardsWithoutSaving()
    {
        var vm = CreateVm();
        var first = MakeSeed(1);
        var second = MakeSeed(2);
        vm.SelectedItem = first;
        vm.EditedItem.SeedsInfo.Note = "Черновик";
        _userDialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        vm.SelectedItem = second;
        Assert.Same(second, vm.SelectedItem);
        Assert.Null(vm.EditedItem.SeedsInfo.Note);
        Assert.Null(first.SeedsInfo.Note);
        _userDialog.Received(1).YesNoQuestion(Arg.Any<string>(), Arg.Any<string>());
    }

    private static Seed MakeSeed(int id) => new()
    {
        Id = id,
        Plant = new Plant
        {
            PlantCulture = new PlantCulture { Name = "Томат" },
            PlantSort = new PlantSort { Id = 1, Name = "Черри", Producer = new Producer { Id = 1, Name = "Гавриш" } }
        },
        SeedsInfo = new SeedsInfo()
    };

    [Fact]
    public void DeleteSeedCommand_Confirmed_RemovesCachedRecordReturnedFromAnotherContext()
    {
        var vm = CreateVm();
        var first = MakeSeed(1);
        vm.Seeds = [first, MakeSeed(2)];
        vm.SelectedItem = first;
        _userDialog.RequestDeleteReason(Arg.Any<string>(), Arg.Any<string?>()).Returns("Истёк срок");
        _seedsService.DeleteSeedWithReasonAsync(1, "Истёк срок").Returns(MakeSeed(1));

        vm.DeleteSeedCommand.Execute(null);

        Assert.DoesNotContain(vm.Seeds, s => s.Id == 1);
        Assert.Contains(vm.Seeds, s => s.Id == 2);
        Assert.Null(vm.SelectedItem);
    }

    [Fact]
    public void UpdateSeedsInfoCommand_ConfirmationRefused_PreservesDraftAndOriginal()
    {
        var vm = CreateVm();
        var seed = MakeSeed(1);
        vm.SelectedItem = seed;
        vm.EditedItem.SeedsInfo.Note = "Черновик";
        vm.SeedSortDetail!.Description = "Общее описание";
        vm.EditedProducerId = 2;
        _userDialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        vm.UpdateSeedsInfoCommand.Execute(null);

        Assert.Null(seed.SeedsInfo.Note);
        Assert.Null(seed.Plant.PlantSort.Description);
        Assert.Equal(1, seed.Plant.PlantSort.Producer.Id);
        Assert.Equal("Черновик", vm.EditedItem.SeedsInfo.Note);
        _seedsService.DidNotReceive().SaveSeedEditAsync(Arg.Any<Seed>(), Arg.Any<SortEditModel>(), Arg.Any<int>());
    }

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
