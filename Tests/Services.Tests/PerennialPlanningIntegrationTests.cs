using Bonfire.Application.Harvest;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Planning.Regions;
using Bonfire.Domain.Libraries;
using BonfireDB.Entities;
using BonfireDB.Entities.Planning;
using BonfireDB.Harvest;
using BonfireDB.Planning;
using BonfireDB.Planning.Libraries;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class PerennialPlanningIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    private readonly TimeProvider _clock = new Clock();
    public void Dispose() => _fixture.Dispose();
    private DbPerennialPlanning Store() => new(_fixture.Factory);
    private ManagePerennialPlanning Manage() => new(Store(), Store(), new DbPlantingPlans(_fixture.Factory), _clock);

    [Fact]
    public async Task SameIdentity_ThreeSeasonsPlanDeletionDoesNotDeletePlantingOrHarvest()
    {
        var planting = await Prepare(); var operations = Manage(); var plans = new DbPlantingPlans(_fixture.Factory);
        var first = await Plan(2026); var second = await Plan(2027); var third = await Plan(2028);
        foreach (var plan in new[] { first, second, third })
        {
            var preview = (await operations.CalculateAsync(plan.Id, planting.Id, new())).ValueOrThrow();
            Assert.Equal(plan.Definition.SeasonYear != 2026, preview.Snapshot.Projection.Periods.Fruiting is SeasonalPeriod.Known);
            Assert.True((await operations.SaveSeasonAsync(preview)).IsSuccess);
        }
        var harvest = new DbHarvestJournal(_fixture.Factory);
        var source = (await harvest.ReadSourcesAsync(2027, default)).ValueOrThrow().Single();
        Assert.Equal(Option.Some(PerennialPlantingId.Create(planting.Id).ValueOrThrow()), source.Identity.PerennialId);
        Assert.True((await new ManageHarvestJournal(harvest, harvest, _clock).SaveAsync(new(Option.None<int>(), 0, source.Identity, new(2027, 7, 15), 1.25m, Option.None<string>()))).IsSuccess);
        Assert.True((await plans.DeleteAsync(SeasonalPlantingPlanId.Create(second.Id).ValueOrThrow(), second.Version + 1, default)).IsSuccess);
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Set<PerennialPlantingRow>().CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.Set<PerennialSeasonRow>().CountAsync()));
        Assert.Single((await harvest.ReadAsync(default)).ValueOrThrow());
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Set<PlannedBatchRow>().CountAsync()));
        Assert.Equal(100, await _fixture.QueryAsync(db => db.Set<SeedsInfo>().Select(x => x.AmountSeeds).SingleAsync()));
    }

    [Fact]
    public async Task ProfileEdit_DoesNotRewriteSavedSeason_AndStalePreviewCannotSave()
    {
        var planting = await Prepare(); var plan = await Plan(2027); var operations = Manage();
        var original = (await operations.CalculateAsync(plan.Id, planting.Id, new())).ValueOrThrow();
        await operations.SaveSeasonAsync(original);
        var definition = planting.Definition;
        var changed = PerennialPlantingDefinition.Create(definition.Name, definition.EstablishmentYear, definition.CultureId, definition.SortId,
            definition.CultureName, definition.SortName, definition.RegionId, definition.RegionName, definition.TimeZoneId,
            new(definition.Profile.Value with { FruitingLater = Option.Some(AnnualDateWindow.Create(6, 1, 8, 1, false).ValueOrThrow()) }, ParameterSource.Batch), definition.FirstEmergence).ValueOrThrow();
        var edited = (await operations.SaveAsync(Option.Some(planting.Id), planting.Version, changed)).ValueOrThrow();
        Assert.Equal(planting.Id, edited.Id);
        Assert.Equal(definition, (await Store().ReadSeasonsAsync(plan.Id, default)).ValueOrThrow().Single().Snapshot.Definition);
        Assert.True((await operations.SaveSeasonAsync(original)).IsFailure);
        var fresh = (await operations.CalculateAsync(plan.Id, planting.Id, new())).ValueOrThrow();
        Assert.Equal(changed, fresh.Snapshot.Definition);
        Assert.True((await operations.SaveSeasonAsync(fresh)).IsSuccess);
    }

    [Fact]
    public async Task DeletePlanting_KeepsJournalSnapshotAndRemovesOnlySeasonRepresentations()
    {
        var planting = await Prepare(); var plan = await Plan(2027); var operations = Manage();
        await operations.SaveSeasonAsync((await operations.CalculateAsync(plan.Id, planting.Id, new())).ValueOrThrow());
        var harvest = new DbHarvestJournal(_fixture.Factory); var source = (await harvest.ReadSourcesAsync(2027, default)).ValueOrThrow().Single();
        await new ManageHarvestJournal(harvest, harvest, _clock).SaveAsync(new(Option.None<int>(), 0, source.Identity, new(2027, 7, 15), 2m, Option.Some("")));
        Assert.True((await operations.DeleteAsync(planting.Id, planting.Version)).IsSuccess);
        var entry = (await harvest.ReadAsync(default)).ValueOrThrow().Single();
        Assert.IsType<Option<int>.None>(entry.CurrentPerennialId); Assert.Equal(source.Identity.PerennialId, entry.Entry.Identity.PerennialId);
        Assert.Equal(2m, entry.Entry.Mass.Kg); Assert.Equal(0, await _fixture.QueryAsync(db => db.Set<PerennialSeasonRow>().CountAsync()));
    }

    [Fact]
    public async Task SaveSeasonFailure_RollsBackPlanVersionAndView()
    {
        var planting = await Prepare(); var plan = await Plan(2027); var operations = Manage();
        var preview = (await operations.CalculateAsync(plan.Id, planting.Id, new())).ValueOrThrow();
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectPerennial BEFORE INSERT ON PlannedPerennialSeasons BEGIN SELECT RAISE(ABORT, 'period rejected'); END;"));
        Assert.True((await operations.SaveSeasonAsync(preview)).IsFailure);
        Assert.Equal(plan.Version, await _fixture.QueryAsync(db => db.Set<SeasonalPlantingPlanRow>().Select(x => x.Version).SingleAsync()));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Set<PerennialSeasonRow>().CountAsync()));
    }

    [Fact]
    public async Task LibraryProfile_RoundTripAndChangeDoNotMutatePlantingSnapshot()
    {
        var planting = await Prepare(); var definition = planting.Definition;
        var libraries = new DbPlanningLibraries(_fixture.Factory);
        var profile = CultureGrowingProfile.Create(definition.CultureId, new(), new() { LifeCycle = Option.Some(definition.Profile.Value) }).ValueOrThrow();
        await libraries.SaveCultureProfileAsync(profile, default);
        var read = (await libraries.ReadProfilesAsync(definition.CultureId, definition.SortId, default)).ValueOrThrow();
        Assert.Equal(profile, read.Culture.Match(x => x, () => throw new Exception()));
        var revised = CultureGrowingProfile.Create(definition.CultureId, new(), new() { LifeCycle = Option.Some(definition.Profile.Value with { FirstHarvestSeason = 1 }) }).ValueOrThrow();
        await libraries.SaveCultureProfileAsync(revised, default);
        Assert.Equal(definition, (await Store().ReadAsync(default)).ValueOrThrow().Single().Definition);
    }

    private async Task<PerennialPlantingFromViewModel> Prepare()
    {
        var plant = await _fixture.PrepareAsync(new Plant { PlantCulture = new() { Name = "Земляника", Class = "Ягоды" },
            PlantSort = new() { Name = "Садовая", Producer = new() { Name = "Фирма" } } });
        await _fixture.ExecuteAsync(async db => { var fresh = await db.Plants.SingleAsync(x => x.Id == plant.Id); db.Seeds.Add(new Seed { Plant = fresh, SeedsInfo = new() { AmountSeeds = 100 } }); await db.SaveChangesAsync(); });
        var profile = new LifeCycleProfile { Kind = LifeCycleKind.Perennial, FirstHarvestSeason = 2, FirstFloweringSeason = 2,
            FruitingLater = Option.Some(AnnualDateWindow.Create(7, 1, 9, 1, false).ValueOrThrow()), FloweringLater = Option.Some(AnnualDateWindow.Create(5, 1, 6, 1, false).ValueOrThrow()) };
        var definition = PerennialPlantingDefinition.Create("Ягодная грядка", 2026, CultureId.Create(plant.PlantCulture.Id).ValueOrThrow(), SortId.Create(plant.PlantSort.Id).ValueOrThrow(),
            "Земляника", "Садовая", RegionId.Create(1).ValueOrThrow(), "Томск", "Asia/Tomsk", new(profile, ParameterSource.Batch), Option.None<DateWindow>()).ValueOrThrow();
        return (await Manage().SaveAsync(Option.None<int>(), 0, definition)).ValueOrThrow();
    }
    private async Task<PlantingPlanDetailsFromViewModel> Plan(int year) => (await new DbPlantingPlans(_fixture.Factory).SaveAsync(PlantingPlanWrite.Create(Option.None<SeasonalPlantingPlanId>(), 0,
        SeasonalPlanDefinition.Create("Сезон", year, RegionId.Create(1).ValueOrThrow(), "Томск", "Asia/Tomsk").ValueOrThrow(), []).ValueOrThrow(), default)).ValueOrThrow();
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(2029, 8, 1, 0, 0, 0, TimeSpan.Zero); }
}
