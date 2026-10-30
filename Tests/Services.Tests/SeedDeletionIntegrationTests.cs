using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class SeedDeletionIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    private Task<int> PrepareAsync() => _fixture.QueryAsync(async db =>
    {
        var plant = new Plant
        {
            PlantCulture = new() { Name = "Томат", Class = "Овощи" },
            PlantSort = new() { Name = "Черри", Producer = new() { Name = "Фирма" } }
        };
        var first = new Seed { Plant = plant, SeedsInfo = new() { DisposeComment = "Прежняя", AmountSeeds = 10 } };
        db.Seeds.AddRange(first, new() { Plant = plant, SeedsInfo = new() { AmountSeeds = 20 } });
        await db.SaveChangesAsync();
        db.Seedlings.Add(new() { Plant = plant, SeedId = first.Id, Quantity = 3 });
        await db.SaveChangesAsync();
        return first.Id;
    });

    [Theory]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData("  Истёк срок  ", "Истёк срок")]
    public async Task Delete_Confirmed_PreservesInfoOtherPackageAndSeedling(string reason, string savedReason)
    {
        var id = await PrepareAsync();
        await _fixture.CreateSeedsService().DeleteSeedWithReasonAsync(id, reason);
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Seeds.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.SeedsInfo.CountAsync()));
        Assert.Equal(savedReason, await _fixture.QueryAsync(db => db.SeedsInfo.Where(info => info.AmountSeeds == 10).Select(info => info.DisposeComment).SingleAsync()));
        Assert.Equal(20, await _fixture.QueryAsync(db => db.Seeds.Select(seed => seed.SeedsInfo.AmountSeeds).SingleAsync()));
        Assert.Equal(3, await _fixture.QueryAsync(db => db.Seedlings.Select(seedling => seedling.Quantity).SingleAsync()));
        Assert.Equal(id, await _fixture.QueryAsync(db => db.Seedlings.Select(seedling => seedling.SeedId).SingleAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Plants.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.PlantsSort.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Producers.CountAsync()));
    }

    [Fact]
    public async Task Delete_SaveRejected_RollsBackReasonAndRemoval()
    {
        var id = await PrepareAsync();
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectSeedDeletion BEFORE DELETE ON Seeds BEGIN SELECT RAISE(ABORT, 'deletion rejected'); END;"));
        var result = await _fixture.CreateSeedsService().DeleteSeedWithReasonAsync(id, "Новое");
        Assert.Contains("deletion rejected", result.Match(_ => "не ожидалось", error => error));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.Seeds.CountAsync()));
        Assert.Equal("Прежняя", await _fixture.QueryAsync(db => db.Seeds.Where(seed => seed.Id == id).Select(seed => seed.SeedsInfo.DisposeComment).SingleAsync()));
    }

    [Fact]
    public async Task Delete_MissingSeed_ReturnsFailureWithoutChangingInventory()
    {
        await PrepareAsync();
        var result = await _fixture.CreateSeedsService().DeleteSeedWithReasonAsync(int.MaxValue, "Причина");
        Assert.Equal("Запись семян удалена", result.Match(_ => "не ожидалось", error => error));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.Seeds.CountAsync()));
        Assert.Equal("Прежняя", await _fixture.QueryAsync(db => db.SeedsInfo.Where(info => info.AmountSeeds == 10).Select(info => info.DisposeComment).SingleAsync()));
    }

    [Fact]
    public async Task Delete_RepeatedRequest_DoesNotOverwriteRetainedReason()
    {
        var id = await PrepareAsync();
        var service = _fixture.CreateSeedsService();
        var first = await service.DeleteSeedWithReasonAsync(id, " Первая причина ");
        Assert.Equal(id, first.ValueOrThrow().SeedId);
        var second = await service.DeleteSeedWithReasonAsync(id, "Не сохранять");
        Assert.True(second.IsFailure);
        Assert.Equal("Первая причина", await _fixture.QueryAsync(db => db.SeedsInfo.Where(info => info.AmountSeeds == 10).Select(info => info.DisposeComment).SingleAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Seeds.CountAsync()));
    }
}
