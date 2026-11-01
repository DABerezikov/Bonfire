using Bonfire.Application.Planning;
using System.Collections.Immutable;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Planning.Regions;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Models.Planning;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels;

namespace ViewModels.Tests;

public class PlanningWorkflowTests
{
    [Fact]
    public async Task Steps_MissingInheritedPeriodsExplainWhatToFillBeforeResult()
    {
        _service.ReadContextAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PlanningContextFromViewModel(
                new(RegionId.Create(1).ValueOrThrow(), RegionDefinition.Create("Томск", "Asia/Tomsk", RegionReference.Empty).ValueOrThrow()),
                new(1, "Томат", "Овощи"), 2, "Черри", new(), Option.None<CultureGrowingProfile>(),
                Option.None<SortPlanningSettings>(), [], "v1")));
        var vm = await Model(); vm.EditorStep = 3;
        Assert.False(vm.ResultStepEnabled);
        Assert.Contains("Прорастание", vm.StepGuidance);
        Assert.Contains("Возраст", vm.StepGuidance);
        vm.Draft.Profile.GermMinText = "5"; vm.Draft.Profile.GermMaxText = "10";
        vm.Draft.Profile.AgeMinText = "50"; vm.Draft.Profile.AgeMaxText = "60";
        Assert.True(vm.ResultStepEnabled);
        Assert.False(vm.Draft.Profile.RequireSeedPercentages);
        vm.Draft.ManualSeedCountText = "";
        Assert.True(vm.Draft.Profile.RequireSeedPercentages);
        Assert.Contains("*", vm.Draft.Profile.GerminationPercentLabel);
    }

    [Fact]
    public async Task Steps_BlockInvalidIntervalAndPreserveInputWhenReturning()
    {
        var vm = await Model();
        vm.Draft.TransplantEndText = "19.05.2027";
        Assert.True(vm.DatesStepEnabled);
        Assert.False(vm.QuantityStepEnabled);
        vm.EditorStep = 1;
        Assert.Contains("позже", vm.StepGuidance);
        Assert.False(vm.NextStepCommand.CanExecute(null));
        vm.Draft.TransplantEndText = "25.05.2027";
        Assert.True(vm.QuantityStepEnabled);
        vm.EditorStep = 2;
        vm.Draft.TargetPlantsText = "42";
        vm.PreviousStepCommand.Execute(null);
        Assert.Equal(1, vm.EditorStep);
        Assert.Equal("42", vm.Draft.TargetPlantsText);
        vm.Draft.DirectSow = true;
        Assert.False(vm.QuantityStepEnabled);
        Assert.Contains("Начало", vm.StepGuidance);
    }

    [Fact]
    public async Task Steps_ExplainMissingBedAndRevokeFollowingSteps()
    {
        var vm = await Model();
        vm.EditorStep = 2;
        vm.Draft.Harvest.Mode = HarvestPlanningMode.BedCapacity;
        Assert.False(vm.ParametersStepEnabled);
        Assert.Contains("размер", vm.StepGuidance);
        vm.Draft.Harvest.ApplyBed(1, "Грядка", 2m, .9m);
        Assert.True(vm.ParametersStepEnabled);
        vm.EditorStep = 3;
        vm.Draft.TransplantStartText = "30.05.2027";
        Assert.Equal(1, vm.EditorStep);
        Assert.False(vm.ResultStepEnabled);
        Assert.Equal("2", vm.Draft.Harvest.Length);
    }

    private readonly IPlanningService _service = Substitute.For<IPlanningService>();
    private readonly IUserDialog _dialog = Substitute.For<IUserDialog>();

    public PlanningWorkflowTests()
    {
        _service.ReadContextAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PlanningContextFromViewModel>("Справка не заполнена"));
        _service.ReadPlanAsync(1, Arg.Any<CancellationToken>()).Returns(Result.Success(Option.Some(Plan())));
    }

    [Fact]
    public async Task Calculate_LatePreview_DoesNotReplaceEditedInput()
    {
        var pending = new TaskCompletionSource<Result<PlanningPreviewFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _service.CalculateAsync(Arg.Any<PlanningBatchInputFromViewModel>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        var vm = await Model();
        var input = vm.Draft.BuildInput().ValueOrThrow();
        var calculation = vm.CalculateAsync();
        vm.Draft.TargetPlantsText = "35";
        pending.SetResult(Result.Success(Preview(input)));
        await calculation;
        Assert.Null(vm.Preview);
        Assert.Equal("35", vm.Draft.TargetPlantsText);
    }

    [Fact]
    public async Task Save_LateInput_IsPreservedAndOriginalBecomesSubmittedSnapshot()
    {
        var vm = await Model();
        _service.CalculateAsync(Arg.Any<PlanningBatchInputFromViewModel>(), Arg.Any<CancellationToken>())
            .Returns(call => Result.Success(Preview(call.Arg<PlanningBatchInputFromViewModel>())));
        await vm.CalculateAsync();
        var input = vm.Draft.BuildInput().ValueOrThrow();
        var pending = new TaskCompletionSource<Result<PlantingPlanDetailsFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _service.SaveBatchAsync(Arg.Any<SavePlannedBatchFromViewModel>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        var draft = vm.Draft;
        var saving = vm.SaveAsync();
        draft.TargetPlantsText = "35";
        var document = Preview(input).Document.Match(x => x, () => throw new Exception());
        pending.SetResult(Result.Success(Plan() with { Version = 2, Batches = [new(10, 1, Option.Some(1), Option.Some(2), document)] }));
        await saving;
        Assert.Same(draft, vm.Draft);
        Assert.Equal("35", draft.TargetPlantsText);
        Assert.Equal("20", draft.Original.TargetPlantsText);
        Assert.Equal(Option.Some(10), draft.BatchId);
        Assert.True(draft.IsDirty);
    }

    [Fact]
    public async Task Save_Failure_KeepsDraftAndPreviewForRetry()
    {
        var vm = await Model();
        _service.CalculateAsync(Arg.Any<PlanningBatchInputFromViewModel>(), Arg.Any<CancellationToken>())
            .Returns(call => Result.Success(Preview(call.Arg<PlanningBatchInputFromViewModel>())));
        await vm.CalculateAsync();
        var draft = vm.Draft;
        var preview = vm.Preview;
        _service.SaveBatchAsync(Arg.Any<SavePlannedBatchFromViewModel>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PlantingPlanDetailsFromViewModel>("Ошибка сохранения"));
        await vm.SaveAsync();
        Assert.Same(draft, vm.Draft);
        Assert.Same(preview, vm.Preview);
        Assert.Contains("Ошибка сохранения", vm.Status);
    }

    private async Task<PlantingPlanningViewModel> Model()
    {
        return await CreateModel();
    }

    [Fact]
    public async Task Refresh_CleanPlanReadsNewVersionAfterPerennialChange()
    {
        ConfigureRefresh();
        var vm = new PlantingPlanningViewModel(_service, _dialog, new(_service), TimeProvider.System);
        await vm.OpenPlanAsync(new(1, 1, "Сезон", 2027, "Томск"));
        _service.ReadPlanAsync(1, Arg.Any<CancellationToken>()).Returns(Result.Success(Option.Some(Plan() with { Version = 2 })));
        await vm.LoadAsync();
        Assert.Equal(2, vm.OpenedPlan!.Version);
    }

    [Fact]
    public async Task Refresh_DirtyDraftDoesNotAcceptNewPlanVersionOrLoseChoice()
    {
        ConfigureRefresh(); var vm = await Model(); var draft = vm.Draft; var choice = draft.Choice;
        _service.ReadPlanAsync(1, Arg.Any<CancellationToken>()).Returns(Result.Success(Option.Some(Plan() with { Version = 2 })));
        await vm.LoadAsync();
        Assert.Same(draft, vm.Draft); Assert.Equal(choice, draft.Choice); Assert.Equal(1, vm.OpenedPlan!.Version);
        Assert.Contains("черновик", vm.Status);
    }

    private void ConfigureRefresh()
    {
        SetupRefresh();
    }

    [Fact]
    public async Task Refresh_LatePlanReadPreservesInputEnteredWhileWaiting()
    {
        ConfigureRefresh(); var vm = new PlantingPlanningViewModel(_service, _dialog, new(_service), TimeProvider.System);
        await vm.OpenPlanAsync(new(1, 1, "Сезон", 2027, "Томск")); var draft = vm.Draft;
        var pending = new TaskCompletionSource<Result<Option<PlantingPlanDetailsFromViewModel>>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _service.ReadPlanAsync(1, Arg.Any<CancellationToken>()).Returns(pending.Task);
        var loading = vm.LoadAsync(); draft.TargetPlantsText = "42";
        pending.SetResult(Result.Success(Option.Some(Plan() with { Version = 2 }))); await loading;
        Assert.Same(draft, vm.Draft); Assert.Equal("42", vm.Draft.TargetPlantsText); Assert.Equal(1, vm.OpenedPlan!.Version);
    }

    private void SetupRefresh()
    {
        _service.ReadPlansAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(ImmutableArray.Create(new PlantingPlanSummaryFromViewModel(1, 2, "Сезон", 2027, "Томск"))));
        _service.ReadRegionsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Result.Success(ImmutableArray.Create(new RegionFromViewModel(RegionId.Create(1).ValueOrThrow(), RegionDefinition.Create("Томск", "Asia/Tomsk", RegionReference.Empty).ValueOrThrow()))));
        _service.ReadChoicesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(ImmutableArray<PlanningChoiceFromViewModel>.Empty));
        _service.ReadBedsAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(ImmutableArray<PlanningBedFromViewModel>.Empty));
    }

    private async Task<PlantingPlanningViewModel> CreateModel()
    {
        var vm = new PlantingPlanningViewModel(_service, _dialog, new PlanningLibrariesViewModel(_service), TimeProvider.System);
        await vm.OpenPlanAsync(new(1, 1, "Сезон", 2027, "Томск"));
        vm.Draft.Choice = new(1, "Томат", 2, "Черри");
        vm.Draft.TransplantStartText = "20.05.2027";
        vm.Draft.TransplantEndText = "25.05.2027";
        vm.Draft.ManualSeedCountText = "30";
        return vm;
    }

    internal static PlantingPlanDetailsFromViewModel Plan() => new(1, 1, Option.Some(1),
        SeasonalPlanDefinition.Create("Сезон", 2027, RegionId.Create(1).ValueOrThrow(), "Томск", "Asia/Tomsk").ValueOrThrow(), []);

    internal static PlanningPreviewFromViewModel Preview(PlanningBatchInputFromViewModel input)
    {
        var profile = new EffectiveGrowingProfile(new(DayRange.Create(5, 10).ValueOrThrow(), ParameterSource.Culture),
            Option.Some(new SourcedValue<DayRange>(DayRange.Create(50, 60).ValueOrThrow(), ParameterSource.Culture)), Option.None<SourcedValue<DayRange>>());
        var batch = PlannedBatchDefinition.Create(Bonfire.Domain.Libraries.CultureId.Create(1).ValueOrThrow(),
            Bonfire.Domain.Libraries.SortId.Create(2).ValueOrThrow(), input.Conditions, input.Method, profile, input.TargetPlants, input.Choices).ValueOrThrow();
        var schedule = PlantingSchedule.Calculate(input.Method, profile, input.Choices, Option.None<EffectivePlanningExtras>()).ValueOrThrow();
        var references = new PlanningReferenceSnapshot("Томат", "Черри", "Томск", "Asia/Tomsk", []);
        var document = PlanningBatchDocument.Create(batch, new(), references, new(schedule,
                new SowingRecommendations.Unavailable(LunarUnavailableReason.UnspecifiedGroup, "Нет группы")),
            Option.Some(SeedQuantityCalculation.Manual(input.TargetPlants, PlantCount.Create(30).ValueOrThrow(), Option.None<GerminationPercentage>(), Option.None<LossPercentage>())),
            DateTimeOffset.UtcNow).ValueOrThrow();
        return new(input, "v1", references, Option.Some(document), [], DateTimeOffset.UtcNow);
    }
}
