namespace ViewModels.Tests;

public class NavigationWorkflowTests
{
    private readonly IUserDialog _dialog = Substitute.For<IUserDialog>();

    private MainWindowViewModel CreateVm() => new(
        new SeedsViewModel(Substitute.For<ISeedsService>(), _dialog, Substitute.For<IReportService>()),
        new SeedlingsViewModel(Substitute.For<ISeedlingsService>(), Substitute.For<ISeedsService>(), _dialog, Substitute.For<IReportService>()),
        new LibraryEditorViewModel(Substitute.For<ILibraryService>(), _dialog,
            new SeedsViewModel(Substitute.For<ISeedsService>(), _dialog, Substitute.For<IReportService>()), new PlanningLibrariesViewModel(Substitute.For<IPlanningService>())),
        new GardenPlanViewModel(Substitute.For<IGardenService>(), _dialog, Substitute.For<ISeedlingsService>(), Substitute.For<ISeedsService>(), Substitute.For<IPlantingService>()),
        new PlantingPlanningViewModel(Substitute.For<IPlanningService>(), _dialog, new PlanningLibrariesViewModel(Substitute.For<IPlanningService>()), TimeProvider.System),
        new HarvestJournalViewModel(Substitute.For<IHarvestService>(), Substitute.For<IPlanningService>(), _dialog, TimeProvider.System));

    [Fact]
    public void Navigation_SelectsScreenAndUpdatesActiveFlags()
    {
        var vm = CreateVm();
        vm.ShowSeedViewModelCommand.Execute(null);
        Assert.Same(vm.SeedsViewModel, vm.CurrentViewModel); Assert.True(vm.SeedsViewModel.IsActive);
        vm.ShowSeedlingsViewModelCommand.Execute(null);
        Assert.Same(vm.SeedlingsViewModel, vm.CurrentViewModel); Assert.False(vm.SeedsViewModel.IsActive); Assert.True(vm.SeedlingsViewModel.IsActive);
        vm.ShowLibraryEditorViewModelCommand.Execute(null);
        Assert.Same(vm.LibraryEditorViewModel, vm.CurrentViewModel); Assert.False(vm.SeedlingsViewModel.IsActive);
        vm.ShowGardenPlanViewModelCommand.Execute(null);
        Assert.Same(vm.GardenPlanViewModel, vm.CurrentViewModel); Assert.True(vm.GardenPlanViewModel.IsActive);
    }

    [Theory]
    [InlineData("Library", false)]
    [InlineData("Library", true)]
    [InlineData("Seedlings", false)]
    [InlineData("Garden", false)]
    public void LeavingSeeds_WithDirtyDraft_RequiresDiscardConfirmation(string target, bool confirm)
    {
        var vm = CreateVm(); vm.ShowSeedViewModelCommand.Execute(null);
        vm.SeedsViewModel.SelectedItem = SeedsWorkflowTests.Seed(1).Read();
        vm.SeedsViewModel.EditedItem!.Package.Note = "Черновик";
        _dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(confirm);
        (target switch
        {
            "Library" => vm.ShowLibraryEditorViewModelCommand,
            "Seedlings" => vm.ShowSeedlingsViewModelCommand,
            _ => vm.ShowGardenPlanViewModelCommand
        }).Execute(null);
        Assert.Equal(!confirm, ReferenceEquals(vm.SeedsViewModel, vm.CurrentViewModel));
        Assert.Equal(confirm ? null : "Черновик", vm.SeedsViewModel.EditedItem!.Package.Note);
    }
}
