using Microsoft.EntityFrameworkCore;
using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;
using BonfireDB.Sowing;
using BonfireDB.Entities.GardenPlanning;
using Bonfire.Domain.Garden.SpotStates;

namespace Services.Tests;

public class SeedlingDeletionIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    private Task<(int SeedId, int BatchId)> PrepareAsync() => _fixture.QueryAsync(async db =>
    {
        var plant = new Plant
        {
            PlantCulture = new() { Name = "Томат", Class = "Овощи" },
            PlantSort = new() { Name = "Черри", Producer = new() { Name = "Фирма" } }
        };
        var seed = new Seed { Plant = plant, SeedsInfo = new() { AmountSeeds = 7, AmountSeedsWeight = 1, Note = "Пакет" } };
        db.Seeds.Add(seed);
        await db.SaveChangesAsync();
        var batch = new Seedling { Plant = plant, SeedId = seed.Id, Quantity = 3, Weight = 0.5,
            SeedlingInfos = [new() { SeedlingNumber = 0, Note = "Метаданные" }] };
        db.Seedlings.Add(batch);
        await db.SaveChangesAsync();
        return (seed.Id, batch.Id);
    });

    private Task<Result<SeedlingDeletionResultFromViewModel>> DeleteAsync(int id, SeedlingRemoval removal)
        => new DeleteSeedling(new DbSeedlingDeletionWriter(_fixture.Factory)).ExecuteAsync(new(id, removal));

    [Fact]
    public async Task DeleteWithReturn_StockSaveRejected_PreservesBatchAndStock()
    {
        var (seedId, batchId) = await PrepareAsync();
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectReturn BEFORE UPDATE ON SeedsInfo BEGIN SELECT RAISE(ABORT, 'return rejected'); END;"));
        var result = await DeleteAsync(batchId, new SeedlingRemoval.CorrectSowing());
        Assert.Contains("return rejected", result.Match(_ => "не ожидалось", error => error));
        Assert.True(await _fixture.QueryAsync(db => db.Seedlings.AnyAsync(batch => batch.Id == batchId)));
        Assert.Equal(7, await _fixture.QueryAsync(db => db.Seeds.Where(seed => seed.Id == seedId).Select(seed => seed.SeedsInfo.AmountSeeds).SingleAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
    }

    [Fact]
    public async Task DeleteWithReturn_MissingSource_PreservesBatch()
    {
        var (seedId, batchId) = await PrepareAsync();
        await _fixture.CreateSeedsService().DeleteSeedWithReasonAsync(seedId, "Пакет удалён");
        Assert.True((await DeleteAsync(batchId, new SeedlingRemoval.CorrectSowing())).IsFailure);
        Assert.True(await _fixture.QueryAsync(db => db.Seedlings.AnyAsync(batch => batch.Id == batchId)));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_Choice_DeletesBatchAndChangesOnlyRequestedStock(bool returnSeeds)
    {
        var (seedId, batchId) = await PrepareAsync();
        var result = await DeleteAsync(batchId, returnSeeds ? new SeedlingRemoval.CorrectSowing() : new SeedlingRemoval.Discard());
        Assert.Equal(batchId, result.ValueOrThrow().SeedlingId);
        Assert.Equal(returnSeeds, result.ValueOrThrow().ReturnedStock.Match(_ => true, () => false));
        Assert.Equal(returnSeeds ? 10 : 7, await _fixture.QueryAsync(db => db.Seeds.Where(seed => seed.Id == seedId).Select(seed => seed.SeedsInfo.AmountSeeds).SingleAsync()));
        Assert.Equal(returnSeeds ? 1.5 : 1, await _fixture.QueryAsync(db => db.Seeds.Select(seed => seed.SeedsInfo.AmountSeedsWeight).SingleAsync()));
        Assert.Equal("Пакет", await _fixture.QueryAsync(db => db.Seeds.Select(seed => seed.SeedsInfo.Note).SingleAsync()));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Seedlings.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Plants.CountAsync()));
    }

    [Fact]
    public async Task Delete_WithoutReturnAndMissingSource_DeletesIndependentBatch()
    {
        var (seedId, batchId) = await PrepareAsync();
        await _fixture.CreateSeedsService().DeleteSeedWithReasonAsync(seedId, "Пакет удалён");
        Assert.True((await DeleteAsync(batchId, new SeedlingRemoval.Discard())).IsSuccess);
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Seedlings.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Plants.CountAsync()));
    }

    [Fact]
    public async Task Delete_RepeatedRequest_DoesNotReturnTwice()
    {
        var (_, batchId) = await PrepareAsync();
        Assert.True((await DeleteAsync(batchId, new SeedlingRemoval.CorrectSowing())).IsSuccess);
        Assert.True((await DeleteAsync(batchId, new SeedlingRemoval.CorrectSowing())).IsFailure);
        Assert.Equal(10, await _fixture.QueryAsync(db => db.Seeds.Select(seed => seed.SeedsInfo.AmountSeeds).SingleAsync()));
    }

    [Fact]
    public async Task Delete_BatchDeleteRejected_RollsBackStockAndMetadataLinks()
    {
        var (_, batchId) = await PrepareAsync();
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectBatchDeletion BEFORE DELETE ON Seedlings BEGIN SELECT RAISE(ABORT, 'batch rejected'); END;"));
        var result = await DeleteAsync(batchId, new SeedlingRemoval.CorrectSowing());
        Assert.Contains("batch rejected", result.Match(_ => "не ожидалось", error => error));
        Assert.Equal(7, await _fixture.QueryAsync(db => db.Seeds.Select(seed => seed.SeedsInfo.AmountSeeds).SingleAsync()));
        Assert.Equal("Метаданные", await _fixture.QueryAsync(db => db.Seedlings.SelectMany(batch => batch.SeedlingInfos).Select(info => info.Note).SingleAsync()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_UnknownGramStock_RejectsReturnButAllowsDiscard(bool returnSeeds)
    {
        var (_, batchId) = await PrepareAsync();
        await _fixture.ExecuteAsync(async db => { (await db.SeedsInfo.SingleAsync()).AmountSeedsWeight = null; await db.SaveChangesAsync(); });
        var result = await DeleteAsync(batchId, returnSeeds ? new SeedlingRemoval.CorrectSowing() : new SeedlingRemoval.Discard());
        Assert.Equal(!returnSeeds, result.IsSuccess);
        Assert.Equal(returnSeeds ? 1 : 0, await _fixture.QueryAsync(db => db.Seedlings.CountAsync()));
        Assert.Equal(7, await _fixture.QueryAsync(db => db.SeedsInfo.Select(info => info.AmountSeeds).SingleAsync()));
        Assert.Null(await _fixture.QueryAsync(db => db.SeedsInfo.Select(info => info.AmountSeedsWeight).SingleAsync()));
    }

    [Fact]
    public async Task Delete_CorrectionWithZeroWeight_PreservesUnknownWeightAndHistoricalInfo()
    {
        var (_, batchId) = await PrepareAsync();
        await _fixture.ExecuteAsync(async db =>
        {
            (await db.SeedsInfo.SingleAsync()).AmountSeedsWeight = null;
            (await db.Seedlings.SingleAsync()).Weight = 0;
            await db.SaveChangesAsync();
        });
        Assert.True((await DeleteAsync(batchId, new SeedlingRemoval.CorrectSowing())).IsSuccess);
        Assert.Equal(10, await _fixture.QueryAsync(db => db.SeedsInfo.Select(info => info.AmountSeeds).SingleAsync()));
        Assert.Null(await _fixture.QueryAsync(db => db.SeedsInfo.Select(info => info.AmountSeedsWeight).SingleAsync()));
        Assert.Equal("Метаданные", await _fixture.QueryAsync(db => db.SeedlingInfos.Select(info => info.Note).SingleAsync()));
        Assert.Null(await _fixture.QueryAsync(db => db.SeedlingInfos.Select(info => EF.Property<int?>(info, "SeedlingId")).SingleAsync()));
    }

    [Fact]
    public async Task Delete_PlantedBatchWithoutReturn_PreservesSpotAndHistoricalInfo()
    {
        var (_, batchId) = await PrepareAsync();
        var spotId = await _fixture.QueryAsync(async db =>
        {
            (await db.Seedlings.SingleAsync()).IsPlantedInBed = true;
            var bed = new Bed { Name = "Грядка", Plot = new Garden { Name = "Участок", GardenPlan = new() { Name = "План", Year = 2026 } } };
            var spot = new PlantingSpot { GardenElement = bed, SeedlingInfo = await db.SeedlingInfos.SingleAsync(),
                StateTypeName = nameof(PlantedSpotState), Note = "Посадка" };
            db.PlantingSpots.Add(spot);
            await db.SaveChangesAsync();
            return spot.Id;
        });
        Assert.True((await DeleteAsync(batchId, new SeedlingRemoval.Discard())).IsSuccess);
        var saved = await _fixture.QueryAsync(db => db.PlantingSpots.Include(spot => spot.SeedlingInfo).SingleAsync(spot => spot.Id == spotId));
        Assert.Equal(nameof(PlantedSpotState), saved.StateTypeName);
        Assert.Equal("Посадка", saved.Note);
        Assert.Equal("Метаданные", saved.SeedlingInfo!.Note);
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Seedlings.CountAsync()));
        Assert.Equal(7, await _fixture.QueryAsync(db => db.SeedsInfo.Select(info => info.AmountSeeds).SingleAsync()));
    }
}
