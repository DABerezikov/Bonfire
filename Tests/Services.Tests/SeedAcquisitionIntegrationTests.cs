using Microsoft.EntityFrameworkCore;
using Bonfire.Application.Seeds;

namespace Services.Tests;

public class SeedAcquisitionIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    private static AddSeedRequest Request(string unit = Units.PiecesOption, string producer = "Фирма А")
        => new("Томат", "Сорт", producer, "Овощи", "Куплено", unit, 2, 3, 12m, new(2028, 12, 31), "Первый");

    [Theory]
    [InlineData(Units.PiecesOption)]
    [InlineData(Units.GramsOption)]
    public async Task Add_SameYear_AccumulatesSelectedUnitAndPreservesPackageMetadata(string unit)
    {
        var service = _fixture.CreateSeedsService();
        var first = (await service.AddOrUpdateSeedAsync(Request(unit))).ValueOrThrow();
        var request = Request(unit) with { QuantityInPack = 5, PackCount = 2, Note = "Другой", CostPack = 20, BestBy = new(2028, 1, 2), SeedSource = "Подарено" };
        var saved = (await service.AddOrUpdateSeedAsync(request)).ValueOrThrow();
        Assert.IsType<AcquisitionResultFromViewModel.Replenished>(saved);
        Assert.Equal(first.SeedId, saved.SeedId);
        var actual = (await _fixture.LoadAsync<Seed>(first.SeedId))!;
        Assert.Equal(16, unit == Units.PiecesOption ? actual.SeedsInfo.AmountSeeds : actual.SeedsInfo.AmountSeedsWeight);
        Assert.Equal(2, unit == Units.PiecesOption ? actual.SeedsInfo.QuantityPack : actual.SeedsInfo.WeightPack);
        Assert.Equal(new DateTime(2028, 12, 31), actual.SeedsInfo.ExpirationDate);
        Assert.Equal("Куплено", actual.SeedsInfo.SeedSource);
        Assert.Equal("Другой", actual.SeedsInfo.Note);
        Assert.Equal(20, actual.SeedsInfo.CostPack);
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Seeds.CountAsync()));
    }

    [Fact]
    public async Task Add_SameSortDifferentProducer_ReassignsSharedSort()
    {
        var service = _fixture.CreateSeedsService();
        var first = (await service.AddOrUpdateSeedAsync(Request())).ValueOrThrow();
        var cache = await _fixture.LoadAllAsync<Seed>();
        var second = (await service.AddOrUpdateSeedAsync(Request(producer: "Фирма Б"))).ValueOrThrow();
        var storedFirst = (await _fixture.LoadAsync<Seed>(first.SeedId))!;
        var storedSecond = (await _fixture.LoadAsync<Seed>(second.SeedId))!;
        Assert.Equal(first.SeedId, second.SeedId);
        Assert.Equal("Фирма Б", storedFirst.Plant.PlantSort.Producer.Name);
        Assert.Equal("Фирма Б", storedSecond.Plant.PlantSort.Producer.Name);
        Assert.Equal("Фирма А", cache[0].Plant.PlantSort.Producer.Name);
        Assert.Equal(1, await _fixture.QueryAsync(db => db.PlantsSort.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.Producers.CountAsync()));
    }

    [Fact]
    public async Task Add_RepeatedRequest_UsesStoredInventoryWithoutCallerCache()
    {
        var service = _fixture.CreateSeedsService();
        await service.AddOrUpdateSeedAsync(Request());
        await service.AddOrUpdateSeedAsync(Request());
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Seeds.CountAsync()));
        Assert.Equal(12, await _fixture.QueryAsync(db => db.Seeds.Select(s => s.SeedsInfo.AmountSeeds).SingleAsync()));
    }

    [Fact]
    public async Task Add_SaveRejected_DoesNotMutateCallerCacheOrStoredGraph()
    {
        var service = _fixture.CreateSeedsService();
        await service.AddOrUpdateSeedAsync(Request());
        var cache = await _fixture.LoadAllAsync<Seed>();
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectAcquisition BEFORE UPDATE ON SeedsInfo BEGIN SELECT RAISE(ABORT, 'acquisition rejected'); END;"));
        var result = await service.AddOrUpdateSeedAsync(Request(producer: "Фирма Б"));
        Assert.Contains("acquisition rejected", result.Match(_ => "не ожидалось", error => error));
        Assert.Equal("Фирма А", cache[0].Plant.PlantSort.Producer.Name);
        Assert.Equal(6, cache[0].SeedsInfo.AmountSeeds);
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Producers.CountAsync()));
        Assert.Equal(6, await _fixture.QueryAsync(db => db.Seeds.Select(s => s.SeedsInfo.AmountSeeds).SingleAsync()));
    }

    [Fact]
    public async Task Add_NewGraphRejected_RollsBackAllEntities()
    {
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectNewSeed BEFORE INSERT ON Seeds BEGIN SELECT RAISE(ABORT, 'new seed rejected'); END;"));
        var result = await _fixture.CreateSeedsService().AddOrUpdateSeedAsync(Request());
        Assert.Contains("new seed rejected", result.Match(_ => "не ожидалось", error => error));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Seeds.CountAsync()));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.SeedsInfo.CountAsync()));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Plants.CountAsync()));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.PlantsSort.CountAsync()));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.PlantsCulture.CountAsync()));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Producers.CountAsync()));
    }

    [Fact]
    public async Task Add_GramsWithoutStoredWeight_ReturnsFailureAndPreservesPackage()
    {
        var service = _fixture.CreateSeedsService();
        var first = (await service.AddOrUpdateSeedAsync(Request())).ValueOrThrow();
        var result = await service.AddOrUpdateSeedAsync(Request(Units.GramsOption) with { Note = "Не сохранять" });
        Assert.Contains("Остаток в граммах не указан", result.Match(_ => "не ожидалось", error => error));
        var saved = (await _fixture.LoadAsync<Seed>(first.SeedId))!;
        Assert.Equal(6, saved.SeedsInfo.AmountSeeds);
        Assert.Null(saved.SeedsInfo.AmountSeedsWeight);
        Assert.Equal("Первый", saved.SeedsInfo.Note);
    }

    [Fact]
    public async Task Add_UnknownUnit_ReturnsFailureWithoutWriting()
    {
        var result = await _fixture.CreateSeedsService().AddOrUpdateSeedAsync(Request("Неизвестно"));
        Assert.True(result.IsFailure);
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Seeds.CountAsync()));
    }
}
