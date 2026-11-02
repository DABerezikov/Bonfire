using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using BonfireDB.Entities;
using BonfireDB.Planning;
using BonfireDB.Planning.Libraries;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class SeedShoppingIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task SavedBatchesShareFreshStock_ExportNeverConsumesIt()
    {
        var plan = await Plan();
        var operation = new CreateSeedShoppingList(new DbPlantingPlans(_fixture.Factory), new DbSeedShoppingStockReader(_fixture.Factory), TimeProvider.System);
        var list = (await operation.ExecuteAsync(plan.Id)).ValueOrThrow();
        var item = Assert.Single(list.Items);
        Assert.Equal("Томат", item.Culture); Assert.Equal(50m, item.Deficit.RequiredSeeds);
        Assert.Equal(Option.Some(43m), item.Deficit.BuySeeds);
        Assert.Equal(7, (await _fixture.QueryAsync(db => db.SeedsInfo.SingleAsync())).AmountSeeds);
        await _fixture.ExecuteAsync(async db => { (await db.SeedsInfo.SingleAsync()).AmountSeeds = 40; await db.SaveChangesAsync(); });
        Assert.Equal(Option.Some(10m), Assert.Single((await operation.ExecuteAsync(plan.Id)).ValueOrThrow().Items).Deficit.BuySeeds);
        await _fixture.ExecuteAsync(async db => { (await db.SeedsInfo.SingleAsync()).AmountSeeds = 60; await db.SaveChangesAsync(); });
        Assert.Empty((await operation.ExecuteAsync(plan.Id)).ValueOrThrow().Items);
    }

    [Fact]
    public async Task NegativeStockIsFailure_AndMissingPlanIsFailure()
    {
        var plan = await Plan();
        var operation = new CreateSeedShoppingList(new DbPlantingPlans(_fixture.Factory), new DbSeedShoppingStockReader(_fixture.Factory), TimeProvider.System);
        await _fixture.ExecuteAsync(async db => { (await db.SeedsInfo.SingleAsync()).AmountSeeds = -1; await db.SaveChangesAsync(); });
        Assert.True((await operation.ExecuteAsync(plan.Id)).IsFailure);
        Assert.True((await operation.ExecuteAsync(999999)).IsFailure);
    }

    private async Task<PlantingPlanDetailsFromViewModel> Plan()
    {
        var input = await _fixture.QueryAsync(async db =>
        {
            var plant = new Plant { PlantCulture = new() { Name = "Томат", Class = "Овощи" }, PlantSort = new()
                { Name = "Черри", Producer = new() { Name = "Фирма" }, MinGerminationTime = 5, MaxGerminationTime = 10,
                    AgeOfSeedlings = 50, MaxAgeOfSeedlings = 60 } };
            db.Seeds.Add(new() { Plant = plant, SeedsInfo = new() { AmountSeeds = 7 } }); await db.SaveChangesAsync();
            return PlanningApplicationTests.Input() with { CultureId = plant.PlantCulture.Id, SortId = plant.PlantSort.Id,
                Choices = PlanningChoices.Empty with { ManualSeedCount = Option.Some(PlantCount.Create(30).ValueOrThrow()) } };
        });
        var store = new DbPlantingPlans(_fixture.Factory); var contexts = new DbPlanningContextReader(_fixture.Factory);
        var manage = new ManagePlantingPlans(store, store, contexts, new DbPlanningLibraries(_fixture.Factory));
        var calculate = new CalculatePlannedBatch(contexts, new RecommendSowing(Substitute.For<ILunarTrendCalendar>()), TimeProvider.System);
        var plan = (await manage.CreateAsync("Покупка", 2027, 1)).ValueOrThrow();
        var first = (await calculate.ExecuteAsync(input)).ValueOrThrow();
        plan = (await manage.SaveBatchAsync(new(plan.Id, plan.Version, Option.None<int>(), input, first, false))).ValueOrThrow();
        input = input with { Choices = input.Choices with { ManualSeedCount = Option.Some(PlantCount.Create(20).ValueOrThrow()) } };
        var second = (await calculate.ExecuteAsync(input)).ValueOrThrow();
        return (await manage.SaveBatchAsync(new(plan.Id, plan.Version, Option.None<int>(), input, second, false))).ValueOrThrow();
    }
}
