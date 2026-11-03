using Bonfire.Application.Seeds;
using System.Collections.Immutable;
using Bonfire.Application.Planning;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Application.Harvest;
using Bonfire.Domain.Functional;

namespace ViewModels.Tests;

public class CachedNavigationTests
{
    private static IPlanningService Planning()
    {
        var planning = Substitute.For<IPlanningService>();
        planning.ReadPlansAsync().Returns(Result.Success(ImmutableArray<PlantingPlanSummaryFromViewModel>.Empty));
        planning.ReadChoicesAsync().Returns(Result.Success(ImmutableArray<PlanningChoiceFromViewModel>.Empty));
        planning.ReadRegionsAsync(Arg.Any<bool>()).Returns(Result.Success(ImmutableArray<RegionFromViewModel>.Empty));
        planning.ReadBedsAsync().Returns(Result.Success(ImmutableArray<PlanningBedFromViewModel>.Empty));
        return planning;
    }

    [Fact]
    public void PlanningAndLibraries_EnsureLoadedOnce_AndManualRefreshIsExplicit()
    {
        var service = Planning();
        var vm = new PlantingPlanningViewModel(service, Substitute.For<IUserDialog>(), new PlanningLibrariesViewModel(service), TimeProvider.System, Substitute.For<IReportService>());
        vm.EnsureLoadedCommand.Execute(null); vm.IsActive = true;
        vm.EnsureLoadedCommand.Execute(null); vm.IsActive = false; vm.IsActive = true;
        service.Received(1).ReadPlansAsync(); service.DidNotReceive().RefreshReadCache();
        vm.LoadCommand.Execute(null); service.Received(1).RefreshReadCache(); service.Received(2).ReadPlansAsync();
        var libraries = new PlanningLibrariesViewModel(service);
        libraries.EnsureLoadedCommand.Execute(null); libraries.EnsureLoadedCommand.Execute(null);
        service.Received(1).ReadRegionsAsync(true);
    }

    [Fact]
    public void JournalAndPerennials_ReuseListsUntilRelevantVersionChanges()
    {
        var planning = Planning(); var harvest = Substitute.For<IHarvestService>();
        harvest.ReadAsync().Returns(Result.Success(ImmutableArray<HarvestEntryFromViewModel>.Empty));
        harvest.ReadSourcesAsync(Arg.Any<int>()).Returns(Result.Success(ImmutableArray<HarvestSourceFromViewModel>.Empty));
        var journal = new HarvestJournalViewModel(harvest, planning, Substitute.For<IUserDialog>(), TimeProvider.System);
        journal.EnsureLoadedCommand.Execute(null); journal.EnsureLoadedCommand.Execute(null);
        harvest.Received(1).ReadAsync();
        harvest.ReadVersion.Returns(1); journal.EnsureLoadedCommand.Execute(null); harvest.Received(2).ReadAsync();
        var perennials = Substitute.For<IPerennialPlanningService>();
        perennials.ReadAsync().Returns(Result.Success(ImmutableArray<PerennialPlantingFromViewModel>.Empty));
        var vm = new PerennialPlanningViewModel(perennials, planning, Substitute.For<IUserDialog>(), TimeProvider.System);
        vm.EnsureLoadedCommand.Execute(null); vm.EnsureLoadedCommand.Execute(null); perennials.Received(1).ReadAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedLoadIsRetriedInsteadOfBeingMarkedLoaded(bool choicesFail)
    {
        var planning = Planning();
        if (choicesFail) planning.ReadChoicesAsync().Returns(Result.Failure<ImmutableArray<PlanningChoiceFromViewModel>>("Недоступно"));
        else planning.ReadPlansAsync().Returns(Result.Failure<ImmutableArray<PlantingPlanSummaryFromViewModel>>("Недоступно"));
        var vm = new PlantingPlanningViewModel(planning, Substitute.For<IUserDialog>(), new PlanningLibrariesViewModel(planning), TimeProvider.System, Substitute.For<IReportService>());
        vm.EnsureLoadedCommand.Execute(null); vm.EnsureLoadedCommand.Execute(null);
        planning.Received(2).ReadPlansAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedRelatedSourceLoadDoesNotMarkWholeScreenLoaded(bool perennial)
    {
        var planning = Planning();
        if (perennial)
        {
            var plan = new PlantingPlanSummaryFromViewModel(1, 1, "Сезон", 2027, "Томск");
            planning.ReadPlansAsync().Returns(Result.Success(ImmutableArray.Create(plan)));
            var service = Substitute.For<IPerennialPlanningService>();
            service.ReadAsync().Returns(Result.Success(ImmutableArray<PerennialPlantingFromViewModel>.Empty));
            service.ReadSeasonsAsync(1).Returns(Result.Failure<ImmutableArray<PerennialSeasonFromViewModel>>("Недоступно"));
            var vm = new PerennialPlanningViewModel(service, planning, Substitute.For<IUserDialog>(), TimeProvider.System);
            vm.Plans.Add(plan); vm.SelectedPlan = plan;
            vm.EnsureLoadedCommand.Execute(null); vm.EnsureLoadedCommand.Execute(null);
            service.Received(2).ReadAsync();
        }
        else
        {
            var service = Substitute.For<IHarvestService>();
            service.ReadAsync().Returns(Result.Success(ImmutableArray<HarvestEntryFromViewModel>.Empty));
            service.ReadSourcesAsync(Arg.Any<int>()).Returns(Result.Failure<ImmutableArray<HarvestSourceFromViewModel>>("Недоступно"));
            var vm = new HarvestJournalViewModel(service, planning, Substitute.For<IUserDialog>(), TimeProvider.System);
            vm.EnsureLoadedCommand.Execute(null); vm.EnsureLoadedCommand.Execute(null);
            service.Received(2).ReadAsync();
        }
    }

    [Fact]
    public void Seeds_RepeatedNavigationAndLoadedReuseCollection_AndReloadOnlyChangedArea()
    {
        var service = Substitute.For<ISeedsService>();
        service.ReadSeedsAsync().Returns(new[] { SeedsWorkflowTests.Seed(1) }.ReadSeeds());
        service.GetEditorOptionsAsync().Returns(Result.Success(new SeedEditorOptionsFromViewModel([], [], [])));
        var vm = new SeedsViewModel(service, Substitute.For<IUserDialog>(), Substitute.For<IReportService>());
        vm.LoadDataCommand.Execute(null); var collection = vm.Seeds; var view = vm.SeedsView;
        vm.IsActive = true; vm.IsActive = false; vm.IsActive = true; vm.LoadDataCommand.Execute(null);
        service.Received(1).ReadSeedsAsync(); service.Received(1).GetEditorOptionsAsync();
        Assert.Same(collection, vm.Seeds); Assert.Same(view, vm.SeedsView);
        service.InventoryVersion.Returns(1);
        service.ReadSeedsAsync().Returns(new[] { SeedsWorkflowTests.Seed(1, quantity: 7) }.ReadSeeds());
        vm.IsActive = false; vm.IsActive = true;
        service.Received(2).ReadSeedsAsync(); service.Received(1).GetEditorOptionsAsync();
        Assert.Equal(7, vm.Seeds![0].SeedsInfo.AmountSeeds);
    }

    [Fact]
    public void Seedlings_RepeatedNavigationDoesNotReadOrReplaceLoadedCollection()
    {
        var seeds = Substitute.For<ISeedsService>(); seeds.ReadSeedsAsync().Returns(Array.Empty<Seed>().ReadSeeds());
        var service = Substitute.For<ISeedlingsService>(); service.ReadSeedlingsAsync().Returns(Array.Empty<Seedling>().ReadSeedlings());
        var vm = new SeedlingsViewModel(service, seeds, Substitute.For<IUserDialog>(), Substitute.For<IReportService>());
        vm.LoadDataCommand.Execute(null); var collection = vm.Seedlings;
        vm.IsActive = true; vm.IsActive = false; vm.IsActive = true; vm.LoadDataCommand.Execute(null);
        service.Received(1).ReadSeedlingsAsync(); seeds.Received(1).ReadSeedsAsync();
        Assert.Same(collection, vm.Seedlings);
    }
}
