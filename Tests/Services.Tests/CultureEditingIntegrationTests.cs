using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class CultureEditingIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task UpdateCulture_ValidDetails_UpdatesLinkedInventoryWithoutDuplicatingEntities()
    {
        var id = await _fixture.QueryAsync(async db =>
        {
            var culture = new PlantCulture { Name = "Старое название", Class = "Зелень" };
            var plant = new Plant
            {
                PlantCulture = culture,
                PlantSort = new() { Name = "Сорт", Producer = new() { Name = "Фирма" } }
            };
            db.Seeds.Add(new() { Plant = plant, SeedsInfo = new() { AmountSeeds = 8 } });
            db.Seedlings.Add(new() { Plant = plant, Quantity = 3 });
            db.PlantsCulture.Add(new() { Name = "Другая", Class = "Цветы" });
            await db.SaveChangesAsync();
            return culture.Id;
        });

        var result = await _fixture.CreateLibraryService().UpdateCultureAsync(
            new() { Id = id, Name = " Томат ", Class = "Особый класс" });

        Assert.True(result.IsSuccess);
        Assert.Equal(" Томат ", result.ValueOrThrow().Name);
        var seed = await _fixture.QueryAsync(db => db.Seeds.Include(s => s.SeedsInfo)
            .Include(s => s.Plant).ThenInclude(p => p.PlantCulture).SingleAsync());
        var seedling = await _fixture.QueryAsync(db => db.Seedlings.Include(s => s.Plant).ThenInclude(p => p.PlantCulture).SingleAsync());
        Assert.Equal(id, seed.Plant.PlantCulture.Id);
        Assert.Equal(" Томат ", seed.Plant.PlantCulture.Name);
        Assert.Equal("Особый класс", seedling.Plant.PlantCulture.Class);
        Assert.Equal(8, seed.SeedsInfo.AmountSeeds);
        Assert.Equal(3, seedling.Quantity);
        Assert.Equal(2, await _fixture.QueryAsync(db => db.PlantsCulture.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Plants.CountAsync()));
        Assert.Equal("Цветы", await _fixture.QueryAsync(db => db.PlantsCulture.Where(c => c.Id != id).Select(c => c.Class).SingleAsync()));
    }

    [Fact]
    public async Task UpdateCulture_SaveRejected_ReturnsReasonAndLeavesStoredValuesUnchanged()
    {
        var id = await _fixture.QueryAsync(async db =>
        {
            var culture = new PlantCulture { Name = "Томат", Class = "Овощи" };
            db.PlantsCulture.Add(culture);
            await db.SaveChangesAsync();
            return culture.Id;
        });
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectCulture BEFORE UPDATE ON PlantsCulture BEGIN SELECT RAISE(ABORT, 'culture rejected'); END;"));

        var result = await _fixture.CreateLibraryService().UpdateCultureAsync(
            new() { Id = id, Name = "Астра", Class = "Цветы" });

        Assert.Contains("culture rejected", result.Match(_ => "не ожидалось", error => error));
        var saved = await _fixture.QueryAsync(db => db.PlantsCulture.SingleAsync());
        Assert.Equal("Томат", saved.Name);
        Assert.Equal("Овощи", saved.Class);
    }

    [Fact]
    public async Task UpdateCulture_MissingCulture_DoesNotCreateNewEntry()
    {
        var result = await _fixture.CreateLibraryService().UpdateCultureAsync(
            new() { Id = 999, Name = "Томат", Class = "Овощи" });
        Assert.Equal("Культура не найдена (удалёна?)", result.Match(_ => "не ожидалось", error => error));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.PlantsCulture.CountAsync()));
    }

    [Theory]
    [InlineData(null, "Овощи")]
    [InlineData("", "Овощи")]
    [InlineData(" ", "Овощи")]
    [InlineData("Томат", "")]
    [InlineData("Томат", " ")]
    public async Task UpdateCulture_InvalidDetails_RejectsWithoutChangingStoredCulture(string? name, string plantClass)
    {
        var id = await _fixture.QueryAsync(async db =>
        {
            var culture = new PlantCulture { Name = "Томат", Class = "Овощи" };
            db.PlantsCulture.Add(culture);
            await db.SaveChangesAsync();
            return culture.Id;
        });
        var service = _fixture.CreateLibraryService();

        var result = await service.UpdateCultureAsync(new CultureEditModel { Id = id, Name = name, Class = plantClass });

        Assert.True(result.IsFailure);
        var saved = await _fixture.QueryAsync(db => db.PlantsCulture.SingleAsync());
        Assert.Equal("Томат", saved.Name);
        Assert.Equal("Овощи", saved.Class);
    }
}
