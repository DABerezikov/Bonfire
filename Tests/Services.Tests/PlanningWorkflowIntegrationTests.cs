using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using BonfireDB.Planning;
using BonfireDB.Planning.Libraries;
using Bonfire.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class PlanningWorkflowIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task CreateCalculateSaveDelete_UsesFreshSourcesAndNeverConsumesSeeds()
    {
        var input = await Prepare();
        var store = new DbPlantingPlans(_fixture.Factory);
        var contexts = new DbPlanningContextReader(_fixture.Factory);
        Assert.Single((await contexts.ReadChoicesAsync(default)).ValueOrThrow());
        var manage = new ManagePlantingPlans(store, store, contexts, new DbPlanningLibraries(_fixture.Factory));
        var calculate = new CalculatePlannedBatch(contexts, new RecommendSowing(new LunarTrendCalendar(new AstronomyLunarPhaseCalculator())), TimeProvider.System);
        var plan = (await manage.CreateAsync("Сезон", 2027, 1)).ValueOrThrow();
        var preview = (await calculate.ExecuteAsync(input)).ValueOrThrow();
        var saved = (await manage.SaveBatchAsync(new(plan.Id, plan.Version, Option.None<int>(), input, preview, false))).ValueOrThrow();
        Assert.Single(saved.Batches);
        Assert.Equal(100, (await _fixture.QueryAsync(db => db.SeedsInfo.SingleAsync())).AmountSeeds);
        Assert.True((await manage.SaveBatchAsync(new(plan.Id, plan.Version, Option.None<int>(), input, preview, false))).IsFailure);
        var remaining = (await manage.DeleteBatchAsync(saved.Id, saved.Version, saved.Batches[0].Id)).ValueOrThrow();
        Assert.Empty(remaining.Batches);
        Assert.True((await manage.DeletePlanAsync(remaining.Id, remaining.Version)).IsSuccess);
        Assert.Equal(100, (await _fixture.QueryAsync(db => db.SeedsInfo.SingleAsync())).AmountSeeds);
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Seeds.CountAsync()));
    }

    [Fact]
    public async Task SourceChange_RequiresNewPreviewOrExplicitHistoricalSnapshot()
    {
        var input = await Prepare();
        var store = new DbPlantingPlans(_fixture.Factory);
        var contexts = new DbPlanningContextReader(_fixture.Factory);
        var manage = new ManagePlantingPlans(store, store, contexts, new DbPlanningLibraries(_fixture.Factory));
        var calculate = new CalculatePlannedBatch(contexts, new RecommendSowing(new LunarTrendCalendar(new AstronomyLunarPhaseCalculator())), TimeProvider.System);
        var plan = (await manage.CreateAsync("Сезон", 2027, 1)).ValueOrThrow();
        var preview = (await calculate.ExecuteAsync(input)).ValueOrThrow();
        await _fixture.ExecuteAsync(async db =>
        {
            (await db.PlantsSort.SingleAsync()).Name = "Другое название";
            await db.SaveChangesAsync();
        });
        Assert.True((await manage.SaveBatchAsync(new(plan.Id, plan.Version, Option.None<int>(), input, preview, false))).IsFailure);
        Assert.Empty((await store.ReadAllAsync(default)).ValueOrThrow().Where(x => x.Version != 1));
        var saved = (await manage.SaveBatchAsync(new(plan.Id, plan.Version, Option.None<int>(), input, preview, true))).ValueOrThrow();
        Assert.Equal("Черри", saved.Batches[0].Document.References.SortName);
        Assert.Equal(input.Choices, saved.Batches[0].Document.Batch.Choices);
        var recalculated = (await calculate.ExecuteAsync(input)).ValueOrThrow();
        Assert.Equal("Другое название", recalculated.References.SortName);
        Assert.Equal(input.Choices, recalculated.Document.Match(x => x.Batch.Choices, () => throw new Exception()));
    }

    private Task<PlanningBatchInputFromViewModel> Prepare() => _fixture.QueryAsync(async db =>
    {
        var plant = new Plant { PlantCulture = new() { Name = "Томат", Class = "Овощи" }, PlantSort = new()
        {
            Name = "Черри", Producer = new() { Name = "Фирма" }, MinGerminationTime = 5, MaxGerminationTime = 10,
            AgeOfSeedlings = 50, MaxAgeOfSeedlings = 60
        } };
        db.Seeds.Add(new() { Plant = plant, SeedsInfo = new() { AmountSeeds = 100 } });
        await db.SaveChangesAsync();
        return PlanningApplicationTests.Input() with { CultureId = plant.PlantCulture.Id, SortId = plant.PlantSort.Id,
            Choices = PlanningChoices.Empty with { ManualSeedCount = Option.Some(PlantCount.Create(30).ValueOrThrow()), SowingDate = Option.Some(new DateOnly(2027, 1, 1)) } };
    });
}
