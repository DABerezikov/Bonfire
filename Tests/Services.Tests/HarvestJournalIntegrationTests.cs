using Bonfire.Application.Harvest;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Harvest;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using BonfireDB.Entities;
using BonfireDB.Entities.Planning;
using BonfireDB.Harvest;
using BonfireDB.Planning;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class HarvestJournalIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    private readonly TimeProvider _clock = new FixedClock(new(2027, 8, 1, 0, 0, 0, TimeSpan.Zero));
    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task Crud_VersionsTotalsAndInventoryStayIndependent()
    {
        var identity = await Prepare(true);
        var store = new DbHarvestJournal(_fixture.Factory); var manage = new ManageHarvestJournal(store, store, _clock);
        var first = (await manage.SaveAsync(Request(identity, 1.25m))).ValueOrThrow();
        var second = (await manage.SaveAsync(Request(identity, 2m))).ValueOrThrow();
        var edit = Request(identity, 3.5m) with { Id = Option.Some(first.Id), ExpectedVersion = first.Version, Note = Option.Some("") };
        var saved = (await manage.SaveAsync(edit)).ValueOrThrow();
        Assert.Equal(first.Id, saved.Id); Assert.Equal(2, saved.Version); Assert.Equal(Option.Some(""), saved.Entry.Note);
        Assert.True((await manage.SaveAsync(edit)).IsFailure);
        Assert.True((await manage.DeleteAsync(second.Id, second.Version)).IsSuccess);
        var entries = (await store.ReadAsync(default)).ValueOrThrow();
        Assert.Single(entries); Assert.Equal(3.5m, entries[0].Entry.Mass.Kg);
        Assert.Equal(100, await _fixture.QueryAsync(db => db.Set<SeedsInfo>().Select(x => x.AmountSeeds).SingleAsync()));
    }

    [Fact]
    public async Task PlanAndLibraryDeletion_PreserveFactsAndAllowCorrection()
    {
        var manual = await Prepare(); var plans = new DbPlantingPlans(_fixture.Factory);
        var definition = SeasonalPlanDefinition.Create("Сезон", 2027, RegionId.Create(1).ValueOrThrow(), "Томск", "Asia/Tomsk").ValueOrThrow();
        var plan = (await plans.SaveAsync(PlantingPlanWrite.Create(Option.None<SeasonalPlantingPlanId>(), 0, definition,
            [new(Option.None<PlannedBatchId>(), PlantingPlanStorageTests.Document(manual.CultureId.Value, manual.SortId.Value, true))]).ValueOrThrow(), default)).ValueOrThrow();
        var store = new DbHarvestJournal(_fixture.Factory); var manage = new ManageHarvestJournal(store, store, _clock);
        var source = (await store.ReadSourcesAsync(2027, default)).ValueOrThrow().Single();
        var saved = (await manage.SaveAsync(Request(source.Identity, 2m))).ValueOrThrow();
        Assert.True((await plans.DeleteAsync(SeasonalPlantingPlanId.Create(plan.Id).ValueOrThrow(), plan.Version, default)).IsSuccess);
        await _fixture.ExecuteAsync(async db => { db.Plants.RemoveRange(db.Plants); db.PlantsSort.RemoveRange(db.PlantsSort); db.PlantsCulture.RemoveRange(db.PlantsCulture); await db.SaveChangesAsync(); });
        var read = (await store.ReadAsync(default)).ValueOrThrow().Single();
        Assert.IsType<Option<int>.None>(read.CurrentBatchId); Assert.IsType<Option<int>.None>(read.CurrentCultureId);
        Assert.Equal(source.Identity.BatchId, read.Entry.Identity.BatchId); Assert.Equal("Томат", read.Entry.Identity.CultureName);
        var corrected = (await manage.SaveAsync(Request(read.Entry.Identity, 3m) with { Id = Option.Some(saved.Id), ExpectedVersion = saved.Version })).ValueOrThrow();
        Assert.Equal(3m, corrected.Entry.Mass.Kg);
    }

    [Fact]
    public async Task DatabaseFailure_DoesNotCreateEntry()
    {
        var identity = await Prepare(); var store = new DbHarvestJournal(_fixture.Factory);
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectHarvest BEFORE INSERT ON HarvestJournalEntries BEGIN SELECT RAISE(ABORT, 'harvest rejected'); END;"));
        var result = await new ManageHarvestJournal(store, store, _clock).SaveAsync(Request(identity, 2m));
        Assert.Contains("harvest rejected", result.Match(_ => "нет отказа", error => error));
        Assert.Empty((await store.ReadAsync(default)).ValueOrThrow());
    }

    [Fact]
    public async Task FutureDate_UsesRegionDayAndRejectsBeforeWriter()
    {
        var identity = await Prepare(); var writer = Substitute.For<IHarvestJournalWriter>();
        writer.SaveAsync(Arg.Any<HarvestWrite>(), Arg.Any<CancellationToken>()).Returns(call => Result.Success(new HarvestEntryFromViewModel(1, 1,
            call.Arg<HarvestWrite>().Entry, Option.None<int>(), Option.Some(identity.CultureId.Value), Option.Some(identity.SortId.Value), Option.None<int>())));
        var operation = new ManageHarvestJournal(Substitute.For<IHarvestJournalReader>(), writer, new FixedClock(new(2026, 11, 1, 20, 30, 0, TimeSpan.Zero)));
        var tomorrowTomsk = Request(identity, 1m) with { Date = new(2026, 11, 2) };
        Assert.True((await operation.SaveAsync(tomorrowTomsk)).IsSuccess);
        var utc = HarvestIdentity.Create(identity.CultureId, identity.SortId, identity.CultureName, identity.SortName, 2027,
            "Томск", "Etc/UTC", Option.None<PlannedBatchId>(), Option.None<PerennialPlantingId>()).ValueOrThrow();
        Assert.True((await operation.SaveAsync(tomorrowTomsk with { Identity = utc })).IsFailure);
        await writer.Received(1).SaveAsync(Arg.Any<HarvestWrite>(), Arg.Any<CancellationToken>());
    }

    private async Task<HarvestIdentity> Prepare(bool seeds = false)
    {
        var plant = await _fixture.PrepareAsync(new Plant { PlantCulture = new() { Name = "Томат", Class = "Овощи" },
            PlantSort = new() { Name = "Черри", Producer = new() { Name = "Фирма" } } });
        if (seeds) await _fixture.ExecuteAsync(async db =>
        {
            var fresh = await db.Plants.SingleAsync(x => x.Id == plant.Id);
            db.Seeds.Add(new Seed { Plant = fresh, SeedsInfo = new() { AmountSeeds = 100 } }); await db.SaveChangesAsync();
        });
        return HarvestIdentity.Create(CultureId.Create(plant.PlantCulture.Id).ValueOrThrow(), SortId.Create(plant.PlantSort.Id).ValueOrThrow(),
            "Томат", "Черри", 2027, "Томск", "Asia/Tomsk", Option.None<PlannedBatchId>(), Option.None<PerennialPlantingId>()).ValueOrThrow();
    }
    private static SaveHarvestFromViewModel Request(HarvestIdentity identity, decimal kg) => new(Option.None<int>(), 0, identity, new(2027, 7, 25), kg, Option.None<string>());
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
}
