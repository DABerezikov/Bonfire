using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Models.Planning;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels;

namespace ViewModels.Tests;

public class PlanningWorkflowTests
{
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
