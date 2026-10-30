using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Domain.Seedlings;
using Bonfire.Domain.Seeds;
using BonfireDB.Planting;
using Bonfire.Domain.Garden.SpotStates;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public partial class PlantingServiceTests
{
    private Task<Result<PlantingResultFromViewModel>> WriteAsync(int spot, PlantingSource source, double amount = 2, CancellationToken token = default)
        => new DbPlantingWriter(_fx.Factory).ApplyAsync(SpotPlanting.Create(spot, source, amount, PlantedDate, " Огурец ", Option.Some("")).ValueOrThrow(), "Фаза", token);

    [Fact]
    public async Task Writer_Seedling_ReturnsFreshRemainingAndPreservesMetadata()
    {
        var spot = await SeedSpotAsync();
        var id = await SeedSeedlingAsync(germinated: 4);
        await _fx.ExecuteAsync(async db =>
        {
            var batch = await db.Seedlings.SingleAsync();
            batch.PlantedOut = 1;
            await db.SaveChangesAsync();
        });
        var receipt = (await WriteAsync(spot, new PlantingSource.Seedlings(SeedlingId.Create(id).ValueOrThrow()), 1.5)).ValueOrThrow();
        Assert.Equal(2, receipt.Consumed.Value);
        Assert.Equal(1, receipt.Remaining.Value);
        Assert.Equal(SeedUnit.Pieces, receipt.Consumed.Unit);
        var info = await _fx.QueryAsync(db => db.SeedlingInfos.SingleAsync(i => i.Id == receipt.SeedlingInfoId));
        Assert.Equal("Фаза", info.LunarPhase);
        Assert.Equal("", info.PlantPlace);
        Assert.Equal(" Огурец ", await _fx.QueryAsync(db => db.PlantingSpots.Select(s => s.Note).SingleAsync()));
    }

    [Theory]
    [InlineData(SeedUnit.Grams, 3.5, 10, 3.5, 6.5)]
    [InlineData(SeedUnit.Pieces, 5, 2, 2, 0)]
    [InlineData(SeedUnit.Pieces, 1.5, 10, 2, 8)]
    public async Task Writer_Seed_ReturnsActualConsumption(SeedUnit unit, double requested, double available, double consumed, double remaining)
    {
        var spot = await SeedSpotAsync();
        var id = await SeedSeedAsync(amountSeeds: available, amountWeight: available);
        var receipt = (await WriteAsync(spot, new PlantingSource.Seeds(SeedId.Create(id).ValueOrThrow(), unit), requested)).ValueOrThrow();
        Assert.Equal(consumed, receipt.Consumed.Value);
        Assert.Equal(remaining, receipt.Remaining.Value);
        Assert.Equal(unit, receipt.Remaining.Unit);
        var batch = await _fx.QueryAsync(db => db.Seedlings.SingleAsync());
        Assert.Equal(unit == SeedUnit.Pieces ? consumed : 0, batch.Quantity);
        Assert.Equal(unit == SeedUnit.Grams ? consumed : 0, batch.Weight);
    }

    [Fact]
    public async Task Writer_UnknownWeight_ReturnsExplicitFailure()
    {
        var spot = await SeedSpotAsync();
        var id = await SeedSeedAsync();
        var result = await WriteAsync(spot, new PlantingSource.Seeds(SeedId.Create(id).ValueOrThrow(), SeedUnit.Grams));
        Assert.Contains("Вес", result.Match(_ => "", error => error));
        Assert.Equal(0, await _fx.QueryAsync(db => db.Seedlings.CountAsync()));
    }

    [Fact]
    public async Task Writer_SaveRejected_ReturnsFailureAndRollsBackInventory()
    {
        var spot = await SeedSpotAsync();
        var id = await SeedSeedAsync();
        await _fx.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectPlanting BEFORE UPDATE ON PlantingSpots BEGIN SELECT RAISE(ABORT, 'planting rejected'); END;"));
        var result = await WriteAsync(spot, new PlantingSource.Seeds(SeedId.Create(id).ValueOrThrow(), SeedUnit.Pieces));
        Assert.Contains("planting rejected", result.Match(_ => "", error => error));
        Assert.Equal(20, await _fx.QueryAsync(db => db.SeedsInfo.Select(s => s.AmountSeeds).SingleAsync()));
        Assert.Equal(0, await _fx.QueryAsync(db => db.Seedlings.CountAsync()));
        Assert.Equal(nameof(EmptySpotState), await _fx.QueryAsync(db => db.PlantingSpots.Select(s => s.StateTypeName).SingleAsync()));
    }
}
