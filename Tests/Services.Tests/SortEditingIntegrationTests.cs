using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class SortEditingIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    private Task<(int Sort, int Culture)> PrepareAsync() => _fixture.QueryAsync(async db =>
    {
        var sort = new PlantSort
        {
            Name = "Сорт", Description = "Описание", PlantHeight = 80,
            Producer = new() { Name = "Фирма" }
        };
        var first = new Plant { PlantSort = sort, PlantCulture = new() { Name = "Томат", Class = "Овощи" } };
        var second = new Plant { PlantSort = sort, PlantCulture = new() { Name = "Перец", Class = "Овощи" } };
        var target = new PlantCulture { Name = "Астра", Class = "Цветы" };
        db.PlantsCulture.Add(target);
        db.Seeds.Add(new() { Plant = first, SeedsInfo = new() { AmountSeeds = 8 } });
        db.Seedlings.Add(new() { Plant = second, Quantity = 3 });
        db.Plants.Add(new()
        {
            PlantSort = new() { Name = "Другой сорт", Producer = sort.Producer },
            PlantCulture = first.PlantCulture
        });
        await db.SaveChangesAsync();
        return (sort.Id, target.Id);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateSort_ValidEdit_PreservesInventoryAndChangesOnlyRequestedFields(bool changeCulture)
    {
        var (id, culture) = await PrepareAsync();
        var result = await _fixture.CreateLibraryService().UpdateSortAsync(new()
        {
            Id = id, Name = " Новое ", CultureId = changeCulture ? culture : null,
            Description = "Игнорируется", PlantHeight = 100
        });
        Assert.True(result.IsSuccess);
        var saved = await _fixture.QueryAsync(db => db.PlantsSort.Include(s => s.Producer)
            .Include(s => s.Plants).ThenInclude(p => p.PlantCulture).SingleAsync(s => s.Id == id));
        Assert.Equal("Новое", saved.Name);
        Assert.Equal("Описание", saved.Description);
        Assert.Equal(80, saved.PlantHeight);
        Assert.Equal("Фирма", saved.Producer.Name);
        Assert.Equal(2, saved.Plants.Count);
        if (changeCulture) Assert.All(saved.Plants, p => Assert.Equal(culture, p.PlantCulture.Id));
        else Assert.Equal(new[] { "Перец", "Томат" }, saved.Plants.Select(p => p.PlantCulture.Name).Order().ToArray());
        Assert.Equal("Томат", await _fixture.QueryAsync(db => db.Plants.Where(p => p.PlantSort.Id != id)
            .Select(p => p.PlantCulture.Name).SingleAsync()));
        Assert.Equal(8, await _fixture.QueryAsync(db => db.Seeds.Select(s => s.SeedsInfo.AmountSeeds).SingleAsync()));
        Assert.Equal(3, await _fixture.QueryAsync(db => db.Seedlings.Select(s => s.Quantity).SingleAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.PlantsSort.CountAsync()));
        Assert.Equal(3, await _fixture.QueryAsync(db => db.Plants.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Producers.CountAsync()));
        var response = result.ValueOrThrow();
        Assert.Equal("Новое", response.Name);
        Assert.Equal(saved.Plants.Select(p => p.Id).Order(), response.Plants.Select(p => p.PlantId).Order());
        Assert.Equal(saved.Plants.Select(p => p.PlantCulture.Name).Order(), response.Plants.Select(p => p.Culture.Name).Order());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task UpdateSort_InvalidName_DoesNotChangeStoredNameOrCulture(string? name)
    {
        var (id, culture) = await PrepareAsync();
        var result = await _fixture.CreateLibraryService().UpdateSortAsync(new() { Id = id, Name = name, CultureId = culture });
        Assert.True(result.IsFailure);
        Assert.Equal("Сорт", await _fixture.QueryAsync(db => db.PlantsSort.Where(s => s.Id == id).Select(s => s.Name).SingleAsync()));
        Assert.Equal(new[] { "Перец", "Томат" }, await _fixture.QueryAsync(db => db.Plants
            .Where(p => p.PlantSort.Id == id).Select(p => p.PlantCulture.Name).OrderBy(n => n).ToArrayAsync()));
    }

    [Fact]
    public async Task UpdateSort_MissingSort_DoesNotCreateEntry()
    {
        var result = await _fixture.CreateLibraryService().UpdateSortAsync(new() { Id = 999, Name = "Сорт" });
        Assert.Equal("Сорт не найден (удалён?)", result.Match(_ => "не ожидалось", error => error));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.PlantsSort.CountAsync()));
    }

    [Fact]
    public async Task UpdateSort_NoPlants_RenamesWithoutInventingCultureLink()
    {
        var id = await _fixture.QueryAsync(async db =>
        {
            var sort = new PlantSort { Name = "Сорт", Producer = new() { Name = "Фирма" } };
            db.PlantsSort.Add(sort);
            await db.SaveChangesAsync();
            return sort.Id;
        });
        var result = await _fixture.CreateLibraryService().UpdateSortAsync(new() { Id = id, Name = "Новое" });
        Assert.Equal("Новое", result.ValueOrThrow().Name);
        Assert.Empty(result.ValueOrThrow().Plants);
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Plants.CountAsync()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateSort_RejectedEdit_LeavesNameAndAllCultureLinksUnchanged(bool rejectSave)
    {
        var (id, culture) = await PrepareAsync();
        if (rejectSave)
            await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER RejectSort BEFORE UPDATE ON PlantsSort BEGIN SELECT RAISE(ABORT, 'sort rejected'); END;"));
        var result = await _fixture.CreateLibraryService().UpdateSortAsync(new()
            { Id = id, Name = "Новое", CultureId = rejectSave ? culture : 999 });
        Assert.Contains(rejectSave ? "sort rejected" : "Культура не найдена", result.Match(_ => "не ожидалось", error => error));
        Assert.Equal("Сорт", await _fixture.QueryAsync(db => db.PlantsSort.Where(s => s.Id == id).Select(s => s.Name).SingleAsync()));
        Assert.Equal(new[] { "Перец", "Томат" }, await _fixture.QueryAsync(db => db.Plants
            .Where(p => p.PlantSort.Id == id).Select(p => p.PlantCulture.Name).OrderBy(n => n).ToArrayAsync()));
    }
}
