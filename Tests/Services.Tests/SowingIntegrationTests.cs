using Bonfire.Domain.Seeds;
using Microsoft.EntityFrameworkCore;
using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;

namespace Services.Tests;

public class SowingIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    private SeedlingsService Service => _fixture.CreateSeedlingsService();
    public void Dispose() => _fixture.Dispose();

    private Task<int> AddSeedAsync(double pieces = 10, double? grams = 1.5) => _fixture.QueryAsync(async db =>
    {
        var seed = new Seed
        {
            Plant = new()
            {
                PlantCulture = new() { Name = "Томат", Class = "Овощи" },
                PlantSort = new() { Name = "Сорт", Producer = new() { Name = "Производитель" } }
            },
            SeedsInfo = new() { AmountSeeds = pieces, AmountSeedsWeight = grams }
        };
        db.Seeds.Add(seed);
        await db.SaveChangesAsync();
        return seed.Id;
    });

    private static SowingRequest Request(int id, SeedUnit unit = SeedUnit.Pieces, double amount = 2)
        => new(id, amount, unit, new DateTime(2026, 3, 1), PlantSources.FromSeeds, Option.Some("Окно"));

    [Theory]
    [InlineData(SeedUnit.Pieces, 2, 8, 1.5)]
    [InlineData(SeedUnit.Grams, 0.5, 10, 1)]
    public async Task Sow_ValidRequest_PersistsBatchAndSelectedBalance(SeedUnit unit, double amount, double pieces, double grams)
    {
        var id = await AddSeedAsync();
        var receipt = (await Service.SowAsync(Request(id, unit, amount))).ValueOrThrow();

        var seed = await _fixture.QueryAsync(db => db.Seeds.Include(s => s.SeedsInfo).SingleAsync());
        var batch = await _fixture.QueryAsync(db => db.Seedlings.Include(s => s.SeedlingInfos).SingleAsync());
        Assert.Equal(pieces, seed.SeedsInfo.AmountSeeds);
        Assert.Equal(grams, seed.SeedsInfo.AmountSeedsWeight);
        Assert.Equal(id, batch.SeedId);
        Assert.Equal(unit == SeedUnit.Pieces ? amount : 0, batch.Quantity);
        Assert.Equal(unit == SeedUnit.Grams ? amount : 0, batch.Weight);
        Assert.Equal(receipt.Saved.BatchId, batch.Id);
        Assert.Equal("Окно", batch.PlantPlace);
        Assert.Equal(Request(id).Date, batch.LandingDate);
        Assert.Equal(batch.LunarPhase, Assert.Single(batch.SeedlingInfos).LunarPhase);
        Assert.Equal(batch.SeedlingSource, batch.SeedlingInfos[0].SeedlingSource);
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Plants.CountAsync()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public async Task Sow_InvalidAmount_LeavesInventoryUnchanged(double amount)
    {
        var id = await AddSeedAsync();
        Assert.True((await Service.SowAsync(Request(id, amount: amount))).IsFailure);
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Seedlings.CountAsync()));
        Assert.Equal(10, await _fixture.QueryAsync(db => db.SeedsInfo.Select(s => s.AmountSeeds).SingleAsync()));
    }

    [Fact]
    public async Task Sow_MissingWeight_ReturnsExplicitFailure()
    {
        var id = await AddSeedAsync(grams: null);
        Assert.True((await Service.SowAsync(Request(id, SeedUnit.Grams, 0.5))).IsFailure);
        Assert.Null(await _fixture.QueryAsync(db => db.SeedsInfo.Select(s => s.AmountSeedsWeight).SingleAsync()));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Seedlings.CountAsync()));
    }

    [Fact]
    public async Task Sow_MissingSeed_ReturnsExplicitFailure()
        => Assert.True((await Service.SowAsync(Request(999))).IsFailure);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sow_PiecesWithAbsentOrZeroWeight_PreservesWeightMeaning(bool knownZero)
    {
        var id = await AddSeedAsync(grams: knownZero ? 0 : null);
        var result = (await Service.SowAsync(Request(id))).ValueOrThrow();
        Assert.Equal(knownZero, result.Saved.RemainingGrams is Option<double>.Some);
        if (knownZero) Assert.Equal(0, result.Saved.RemainingGrams.Match(value => value, () => -1));
        var persisted = await _fixture.QueryAsync(db => db.SeedsInfo.Select(s => s.AmountSeedsWeight).SingleAsync());
        Assert.Equal(knownZero ? 0d : (double?)null, persisted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sow_AbsentOrBlankPlace_PersistsNullAtStorageBoundary(bool blank)
    {
        var id = await AddSeedAsync();
        var place = blank ? Option.Some("  ") : Option.None<string>();
        var receipt = (await Service.SowAsync(Request(id) with { Place = place })).ValueOrThrow();
        Assert.IsType<Option<string>.None>(receipt.Details.Place);
        Assert.Null(await _fixture.QueryAsync(db => db.Seedlings.Select(s => s.PlantPlace).SingleAsync()));
        Assert.Null(await _fixture.QueryAsync(db => db.SeedlingInfos.Select(s => s.PlantPlace).SingleAsync()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Sow_MissingDateOrSource_LeavesInventoryUnchanged(bool missingDate)
    {
        var id = await AddSeedAsync();
        var request = missingDate ? Request(id) with { Date = default } : Request(id) with { Source = " " };
        Assert.True((await Service.SowAsync(request)).IsFailure);
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Seedlings.CountAsync()));
        Assert.Equal(10, await _fixture.QueryAsync(db => db.SeedsInfo.Select(s => s.AmountSeeds).SingleAsync()));
    }

    [Fact]
    public async Task Sow_UnknownUnit_LeavesInventoryUnchanged()
    {
        var id = await AddSeedAsync();
        Assert.True((await Service.SowAsync(Request(id, (SeedUnit)42))).IsFailure);
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Seedlings.CountAsync()));
    }

    [Fact]
    public async Task Sow_StaleRequest_RechecksCurrentBalance()
    {
        var id = await AddSeedAsync(pieces: 3);
        Assert.True((await Service.SowAsync(Request(id))).IsSuccess);
        Assert.True((await Service.SowAsync(Request(id))).IsFailure);
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Seedlings.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.SeedsInfo.Select(s => s.AmountSeeds).SingleAsync()));
    }

    [Fact]
    public async Task Sow_DatabaseRejectsBalanceChange_RollsBackBatchAndBalance()
    {
        var id = await AddSeedAsync();
        // Ошибка внутри реального SaveChanges после начала записи графа.
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectSowing BEFORE UPDATE ON SeedsInfo BEGIN SELECT RAISE(ABORT, 'test failure'); END;"));

        await Assert.ThrowsAsync<DbUpdateException>(() => Service.SowAsync(Request(id)));

        Assert.Equal(0, await _fixture.QueryAsync(db => db.Seedlings.CountAsync()));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
        Assert.Equal(10, await _fixture.QueryAsync(db => db.SeedsInfo.Select(s => s.AmountSeeds).SingleAsync()));
    }
}
