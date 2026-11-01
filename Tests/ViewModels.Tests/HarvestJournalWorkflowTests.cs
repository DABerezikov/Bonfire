using System.Collections.Immutable;
using Bonfire.Application.Harvest;
using Bonfire.Application.Planning;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Harvest;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;

namespace ViewModels.Tests;

public class HarvestJournalWorkflowTests
{
    [Fact]
    public async Task Save_LateInputAndDoubleCall_KeepOneEntryAndNewDraftText()
    {
        var service = Substitute.For<IHarvestService>(); var vm = Model(service);
        var pending = new TaskCompletionSource<Result<HarvestEntryFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.SaveAsync(Arg.Any<SaveHarvestFromViewModel>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        var request = vm.Draft.Build().ValueOrThrow();
        var saving = vm.SaveAsync(); await vm.SaveAsync(); vm.Draft.MassKg = "3,5";
        pending.SetResult(Result.Success(Receipt(request, 1))); await saving;
        Assert.Single(vm.Entries); Assert.Equal("3,5", vm.Draft.MassKg); Assert.Equal("1,25", vm.Draft.Original.MassKg);
        Assert.Equal(Option.Some(1), vm.Draft.Id); Assert.True(vm.Draft.IsDirty);
        await service.Received(1).SaveAsync(Arg.Any<SaveHarvestFromViewModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveFailure_KeepsDraftAndValidationDoesNotConvertTextToZero()
    {
        var service = Substitute.For<IHarvestService>(); var vm = Model(service); var draft = vm.Draft;
        service.SaveAsync(Arg.Any<SaveHarvestFromViewModel>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<HarvestEntryFromViewModel>("SQLite busy"));
        await vm.SaveAsync(); Assert.Same(draft, vm.Draft); Assert.Equal("1,25", draft.MassKg); Assert.Contains("busy", vm.Status);
        draft.MassKg = "не число"; Assert.False(vm.SaveCommand.CanExecute(null)); Assert.Equal("не число", draft.MassKg);
    }

    [Fact]
    public async Task Filters_DoNotCompareUnassignedFactsOrAnotherSeasonToBatch()
    {
        var service = Substitute.For<IHarvestService>(); var vm = Model(service);
        var request = vm.Draft.Build().ValueOrThrow();
        var linked = Receipt(request, 1);
        var owner = HarvestIdentity.Create(request.Identity.CultureId, request.Identity.SortId, "Томат", "Черри", 2027, "Томск", "Asia/Tomsk", Option.None<PlannedBatchId>(), Option.None<PerennialPlantingId>()).ValueOrThrow();
        service.ReadAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(ImmutableArray.Create(linked, Receipt(request with { Identity = owner, MassKg = 10m }, 2))));
        await vm.LoadAsync(); Assert.Contains("11,25", vm.Summary);
        vm.FilterBatch = "7"; Assert.Single(vm.Entries); Assert.Contains("1,25", vm.Summary); Assert.Contains("прогноз", vm.Summary);
        vm.FilterSeason = "2026"; Assert.Empty(vm.Entries); Assert.Contains("недоступен", vm.Summary);
    }

    internal static HarvestJournalViewModel Model(IHarvestService service)
    {
        var source = Source();
        service.ReadAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(ImmutableArray<HarvestEntryFromViewModel>.Empty));
        service.ReadSourcesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Result.Success(ImmutableArray.Create(source)));
        var planning = Substitute.For<IPlanningService>();
        planning.ReadChoicesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success(ImmutableArray<PlanningChoiceFromViewModel>.Empty));
        planning.ReadRegionsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Result.Success(ImmutableArray<RegionFromViewModel>.Empty));
        var vm = new HarvestJournalViewModel(service, planning, Substitute.For<IUserDialog>(), new Clock());
        vm.Sources.Add(source); vm.Draft.Source = source; vm.Draft.Season = "2027"; vm.Draft.Date = "25.07.2027"; vm.Draft.MassKg = "1,25";
        return vm;
    }
    internal static HarvestSourceFromViewModel Source() => new("2027: Томат / Черри, партия #7", HarvestIdentity.Create(CultureId.Create(1).ValueOrThrow(), SortId.Create(2).ValueOrThrow(),
        "Томат", "Черри", 2027, "Томск", "Asia/Tomsk", Option.Some(PlannedBatchId.Create(7).ValueOrThrow()), Option.None<PerennialPlantingId>()).ValueOrThrow(), Option.Some(10m), Option.Some(12m));
    private static HarvestEntryFromViewModel Receipt(SaveHarvestFromViewModel input, int id) => new(id, 1,
        HarvestEntry.Create(input.Identity, input.Date, HarvestMass.Create(input.MassKg).ValueOrThrow(), input.Note, new(2027, 8, 1)).ValueOrThrow(),
        input.Identity.BatchId.Map(x => x.Value), Option.Some(1), Option.Some(2), Option.None<int>());
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(2027, 8, 1, 0, 0, 0, TimeSpan.Zero); }
}
