using System.Collections.Immutable;
using Bonfire.Application.Planning;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.Regions;

namespace ViewModels.Tests;

public class PerennialPlanningWorkflowTests
{
    [Fact]
    public async Task Calculate_LateManualPeriodDoesNotReceiveOldPreview()
    {
        var service = Substitute.For<IPerennialPlanningService>(); var vm = Model(service);
        var pending = new TaskCompletionSource<Result<PerennialSeasonPreview>>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.CalculateAsync(1, 1, Arg.Any<GrowthPeriodOverrides>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        var calculation = vm.CalculateAsync(); vm.Season.FruitStart = "02.07.2027"; vm.Season.FruitEnd = "30.09.2027";
        pending.SetResult(Result.Success(Preview())); await calculation;
        Assert.Null(vm.Preview); Assert.Equal("02.07.2027", vm.Season.FruitStart);
    }

    [Fact]
    public async Task SavePlanting_LateNameRemainsAndVersionIsAccepted()
    {
        var service = Substitute.For<IPerennialPlanningService>(); var vm = Model(service); var draft = vm.Draft;
        draft.Name = "Сохранить"; var definition = draft.Build().ValueOrThrow();
        var pending = new TaskCompletionSource<Result<PerennialPlantingFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.SaveAsync(Arg.Any<Option<int>>(), 1, Arg.Any<PerennialPlantingDefinition>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        var saving = vm.SaveAsync(); draft.Name = "Поздний ввод";
        pending.SetResult(Result.Success(new PerennialPlantingFromViewModel(1, 2, definition, Option.Some(1), Option.Some(2), Option.Some(1)))); await saving;
        Assert.Equal("Поздний ввод", draft.Name); Assert.Equal("Сохранить", draft.Original.Name); Assert.Equal(2, draft.Version); Assert.True(draft.IsDirty);
    }

    [Fact]
    public async Task SaveSeason_LateManualTextSurvivesPlanVersionUpdate()
    {
        var service = Substitute.For<IPerennialPlanningService>(); var vm = Model(service);
        service.CalculateAsync(1, 1, Arg.Any<GrowthPeriodOverrides>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Preview()));
        await vm.CalculateAsync();
        var pending = new TaskCompletionSource<Result<SavedPerennialSeason>>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.SaveSeasonAsync(Arg.Any<PerennialSeasonPreview>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        var season = vm.Season; var saving = vm.SaveSeasonAsync(); season.FruitStart = "02.07.2027"; season.FruitEnd = "30.09.2027";
        pending.SetResult(Result.Success(new SavedPerennialSeason(new(1, 1, 1, 1, Preview().Snapshot), 2))); await saving;
        Assert.Same(season, vm.Season); Assert.Equal("02.07.2027", season.FruitStart); Assert.Equal("", season.Original.FruitStart);
        Assert.Equal(2, vm.SelectedPlan!.Version); Assert.Null(vm.Preview);
    }

    internal static PerennialPlanningViewModel Model(IPerennialPlanningService service)
    {
        var definition = Definition();
        var planting = new PerennialPlantingFromViewModel(1, 1, definition, Option.Some(1), Option.Some(2), Option.Some(1));
        var plan = new PlantingPlanSummaryFromViewModel(1, 1, "Сезон", 2027, "Томск");
        service.ReadAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(ImmutableArray.Create(planting)));
        service.ReadSeasonsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Result.Success(ImmutableArray<PerennialSeasonFromViewModel>.Empty));
        service.CalculateAsync(1, 1, Arg.Any<GrowthPeriodOverrides>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Preview()));
        var planning = Substitute.For<IPlanningService>();
        planning.ReadPlansAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(ImmutableArray.Create(plan)));
        planning.ReadChoicesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(ImmutableArray.Create(new PlanningChoiceFromViewModel(1, "Земляника", 2, "Садовая"))));
        planning.ReadRegionsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Result.Success(ImmutableArray.Create(new RegionFromViewModel(definition.RegionId,
            RegionDefinition.Create("Томск", "Asia/Tomsk", RegionReference.Empty).ValueOrThrow()))));
        var vm = new PerennialPlanningViewModel(service, planning, Substitute.For<IUserDialog>(), TimeProvider.System);
        vm.Plantings.Add(planting); vm.Selected = planting; vm.Plans.Add(plan); vm.SelectedPlan = plan;
        return vm;
    }
    internal static PerennialSeasonPreview Preview()
    {
        var definition = Definition(); var manual = new GrowthPeriodOverrides();
        return new(1, 1, 1, 1, new(definition, manual, SeasonalPhenology.Project(definition.Profile.Value, 2026, 2027, definition.FirstEmergence, manual).ValueOrThrow(), DateTimeOffset.UtcNow));
    }
    internal static PerennialPlantingDefinition Definition() => PerennialPlantingDefinition.Create("Ягодная грядка", 2026, CultureId.Create(1).ValueOrThrow(), SortId.Create(2).ValueOrThrow(),
        "Земляника", "Садовая", RegionId.Create(1).ValueOrThrow(), "Томск", "Asia/Tomsk", new(new LifeCycleProfile { Kind = LifeCycleKind.Perennial,
            FirstHarvestSeason = 2, FirstFloweringSeason = 2, FruitingLater = Option.Some(AnnualDateWindow.Create(7, 1, 9, 1, false).ValueOrThrow()),
            FloweringLater = Option.Some(AnnualDateWindow.Create(5, 1, 6, 1, false).ValueOrThrow()) }, ParameterSource.Batch), Option.None<DateWindow>()).ValueOrThrow();
}
