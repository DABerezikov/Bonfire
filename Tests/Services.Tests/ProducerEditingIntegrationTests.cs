using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class ProducerEditingIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\r\n")]
    public async Task UpdateProducer_InvalidName_RejectsWithoutChangingStoredProducer(string? name)
    {
        var id = await AddProducerAsync();
        var result = await _fixture.CreateLibraryService().UpdateProducerAsync(id, name!);
        Assert.True(result.IsFailure);
        Assert.Equal("Фирма", await _fixture.QueryAsync(db => db.Producers.Select(p => p.Name).SingleAsync()));
    }

    [Fact]
    public async Task UpdateProducer_ValidName_UpdatesLinkedInventoryWithoutDuplicatingEntities()
    {
        var id = await _fixture.QueryAsync(async db =>
        {
            var producer = new Producer { Name = "Фирма" };
            var plant = new Plant
            {
                PlantCulture = new() { Name = "Томат", Class = "Овощи" },
                PlantSort = new() { Name = "Сорт", Producer = producer }
            };
            db.Seeds.Add(new() { Plant = plant, SeedsInfo = new() { AmountSeeds = 8 } });
            db.Seedlings.Add(new() { Plant = plant, Quantity = 3 });
            db.Producers.Add(new() { Name = "Другая фирма" });
            await db.SaveChangesAsync();
            return producer.Id;
        });

        var result = await _fixture.CreateLibraryService().UpdateProducerAsync(id, " Новая фирма ");

        Assert.Equal(" Новая фирма ", result.ValueOrThrow().Name);
        var seed = await _fixture.QueryAsync(db => db.Seeds.Include(s => s.SeedsInfo)
            .Include(s => s.Plant).ThenInclude(p => p.PlantCulture)
            .Include(s => s.Plant).ThenInclude(p => p.PlantSort).ThenInclude(s => s.Producer).SingleAsync());
        var seedling = await _fixture.QueryAsync(db => db.Seedlings
            .Include(s => s.Plant).ThenInclude(p => p.PlantSort).ThenInclude(s => s.Producer).SingleAsync());
        Assert.Equal(id, seed.Plant.PlantSort.Producer.Id);
        Assert.Equal(" Новая фирма ", seed.Plant.PlantSort.Producer.Name);
        Assert.Equal(" Новая фирма ", seedling.Plant.PlantSort.Producer.Name);
        Assert.Equal("Томат", seed.Plant.PlantCulture.Name);
        Assert.Equal("Сорт", seed.Plant.PlantSort.Name);
        Assert.Equal(8, seed.SeedsInfo.AmountSeeds);
        Assert.Equal(3, seedling.Quantity);
        Assert.Equal(2, await _fixture.QueryAsync(db => db.Producers.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Plants.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.PlantsSort.CountAsync()));
        Assert.Equal("Другая фирма", await _fixture.QueryAsync(db => db.Producers
            .Where(p => p.Id != id).Select(p => p.Name).SingleAsync()));
    }

    [Fact]
    public async Task UpdateProducer_SaveRejected_ReturnsReasonAndLeavesStoredNameUnchanged()
    {
        var id = await AddProducerAsync();
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectProducer BEFORE UPDATE ON Producers BEGIN SELECT RAISE(ABORT, 'producer rejected'); END;"));

        var result = await _fixture.CreateLibraryService().UpdateProducerAsync(id, "Новое имя");

        Assert.Contains("producer rejected", result.Match(_ => "не ожидалось", error => error));
        Assert.Equal("Фирма", await _fixture.QueryAsync(db => db.Producers.Select(p => p.Name).SingleAsync()));
    }

    [Fact]
    public async Task UpdateProducer_MissingProducer_DoesNotCreateNewEntry()
    {
        var result = await _fixture.CreateLibraryService().UpdateProducerAsync(999, "Фирма");
        Assert.Equal("Производитель не найден (удалён?)", result.Match(_ => "не ожидалось", error => error));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Producers.CountAsync()));
    }

    private Task<int> AddProducerAsync() => _fixture.QueryAsync(async db =>
    {
        var producer = new Producer { Name = "Фирма" };
        db.Producers.Add(producer);
        await db.SaveChangesAsync();
        return producer.Id;
    });
}
