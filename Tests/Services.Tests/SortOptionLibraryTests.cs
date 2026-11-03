using Bonfire.Application.Libraries;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using BonfireDB.Libraries;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class SortOptionLibraryTests
{
    [Theory]
    [InlineData(SortOptionCategory.Color)]
    [InlineData(SortOptionCategory.Maturity)]
    public async Task RenameUpdatesAllReferences_UsedValueCannotBeDeleted(SortOptionCategory category)
    {
        using var fixture = new SqliteUowFixture();
        await fixture.ExecuteAsync(async db =>
        {
            foreach (var name in new[] { "Старое", " старое " })
                db.PlantsSort.Add(new() { Name = "Сорт", PlantColor = category == SortOptionCategory.Color ? name : "Не менять",
                    Maturity = category == SortOptionCategory.Maturity ? name : "Не менять", Producer = new() { Name = "Фирма" } });
            db.Add(new SortOption { Category = category == SortOptionCategory.Color ? "Цвет" : "Созревание", Name = "СТАРОЕ" });
            await db.SaveChangesAsync();
        });
        var service = new ManageSortOptions(new DbSortOptionStore(fixture.Factory));
        var item = Assert.Single((await service.ReadAsync()).ValueOrThrow().Where(item => item.Category == category));
        Assert.True(item.IsUsed);
        Assert.True((await service.DeleteAsync(category, item.Name)).IsFailure);
        Assert.True((await service.SaveAsync(category, Option.Some(item.Name), "Новое")).IsSuccess);
        var rows = await fixture.QueryAsync(db => db.PlantsSort.AsNoTracking().ToArrayAsync());
        Assert.All(rows, row =>
        {
            Assert.Equal("Новое", category == SortOptionCategory.Color ? row.PlantColor : row.Maturity);
            Assert.Equal("Не менять", category == SortOptionCategory.Color ? row.Maturity : row.PlantColor);
        });
        var values = (await fixture.CreateSeedsService().GetEditorOptionsAsync()).ValueOrThrow();
        Assert.Equal(new[] { "Новое" }, category == SortOptionCategory.Color ? values.Colors : values.Maturities);
        Assert.True((await service.DeleteAsync(category, "Новое")).IsFailure);
    }

    [Fact]
    public async Task AddRenameDeleteUnusedValue_RejectsBlankDuplicateAndStaleChanges()
    {
        using var fixture = new SqliteUowFixture();
        var service = new ManageSortOptions(new DbSortOptionStore(fixture.Factory));
        Assert.True((await service.SaveAsync(SortOptionCategory.Color, Option.None<string>(), " ")).IsFailure);
        Assert.True((await service.SaveAsync((SortOptionCategory)99, Option.None<string>(), "Белый")).IsFailure);
        Assert.True((await service.SaveAsync(SortOptionCategory.Color, Option.None<string>(), " Белый ")).IsSuccess);
        Assert.True((await service.SaveAsync(SortOptionCategory.Color, Option.None<string>(), "БЕЛЫЙ")).IsFailure);
        Assert.True((await service.SaveAsync(SortOptionCategory.Color, Option.Some("Белый"), "Зелёный")).IsSuccess);
        Assert.True((await service.SaveAsync(SortOptionCategory.Color, Option.Some("Белый"), "Красный")).IsFailure);
        Assert.True((await service.DeleteAsync(SortOptionCategory.Color, "Зелёный")).IsSuccess);
        Assert.True((await service.DeleteAsync(SortOptionCategory.Color, "Зелёный")).IsFailure);
        Assert.Empty((await service.ReadAsync()).ValueOrThrow());
    }

    [Fact]
    public async Task CatalogContainsUnusedCulturesSortsAndProducers_AndTheirLinks()
    {
        using var fixture = new SqliteUowFixture();
        await fixture.ExecuteAsync(async db =>
        {
            var culture = new PlantCulture { Name = "Астра", Class = "Цветы" };
            db.Add(new Plant { PlantCulture = culture, PlantSort = new() { Name = "Белая", Producer = new() { Name = "Фирма" } } });
            db.Add(new PlantCulture { Name = "Новая", Class = "Овощи" });
            db.Add(new PlantSort { Name = "Без культуры", Producer = new() { Name = "Новая фирма" } });
            await db.SaveChangesAsync();
        });
        var catalog = Assert.IsType<Option<SeedCatalogFromViewModel>.Some>((await fixture.CreateSeedsService().GetEditorOptionsAsync()).ValueOrThrow().Catalog).Value;
        Assert.Equal(2, catalog.Cultures.Length); Assert.Equal(2, catalog.Sorts.Length); Assert.Equal(2, catalog.Producers.Length);
        var culture = catalog.Cultures.Single(c => c.Name == "Астра");
        Assert.Equal(culture.Id, Assert.Single(catalog.Sorts.Single(s => s.Name == "Белая").CultureIds));
        Assert.Empty(catalog.Sorts.Single(s => s.Name == "Без культуры").CultureIds);
    }

    [Fact]
    public async Task AcquireFromFullCatalog_ReusesReferencesWithoutExistingPackage()
    {
        using var fixture = new SqliteUowFixture();
        var ids = await fixture.QueryAsync(async db =>
        {
            var plant = new Plant { PlantCulture = new() { Name = "Томат", Class = "Овощи" },
                PlantSort = new() { Name = "Черри", PlantColor = "Красный", Producer = new() { Name = "Фирма" } } };
            db.Add(plant); await db.SaveChangesAsync(); return (plant.Id, plant.PlantCulture.Id, plant.PlantSort.Id, plant.PlantSort.Producer.Id);
        });
        var request = new Bonfire.Models.AddSeedRequest("Томат", "Черри", "Фирма", "Овощи", "Куплено", Bonfire.Models.Units.PiecesOption, 10, 1, 0, new DateTime(2028,12,31), "");
        var result = await fixture.CreateSeedsService().AddOrUpdateSeedAsync(request);
        Assert.True(result.IsSuccess, result.Match(_ => "", error => error));
        await fixture.ExecuteAsync(async db =>
        {
            Assert.Equal(1, await db.PlantsCulture.CountAsync()); Assert.Equal(1, await db.PlantsSort.CountAsync());
            Assert.Equal(1, await db.Producers.CountAsync()); Assert.Equal(1, await db.Plants.CountAsync());
            var seed = await db.Seeds.Include(s => s.Plant).ThenInclude(p => p.PlantSort).SingleAsync();
            Assert.Equal(ids.Item1, seed.Plant.Id); Assert.Equal("Красный", seed.Plant.PlantSort.PlantColor);
        });
    }

    [Theory]
    [InlineData(LibraryEntryKind.Culture)]
    [InlineData(LibraryEntryKind.Sort)]
    [InlineData(LibraryEntryKind.Producer)]
    public async Task DeleteLibraryEntry_ProtectsReferences_AndAllowsUnused(LibraryEntryKind kind)
    {
        using var fixture = new SqliteUowFixture();
        var ids = await fixture.QueryAsync(async db =>
        {
            var plant = new Plant { PlantCulture = new() { Name = "Томат", Class = "Овощи" }, PlantSort = new() { Name = "Черри", Producer = new() { Name = "Фирма" } } };
            db.Add(plant); await db.SaveChangesAsync();
            return (plant.PlantCulture.Id, plant.PlantSort.Id, plant.PlantSort.Producer.Id);
        });
        var service = fixture.CreateLibraryService();
        var usedId = kind switch { LibraryEntryKind.Culture => ids.Item1, LibraryEntryKind.Sort => ids.Item2, _ => ids.Item3 };
        Assert.True((await service.DeleteEntryAsync(kind, usedId)).IsFailure);
        var unusedId = await fixture.QueryAsync(async db =>
        {
            BonfireDB.Entities.Base.Entity entry = kind switch
            {
                LibraryEntryKind.Culture => new PlantCulture { Name = "Пустая", Class = "Овощи" },
                LibraryEntryKind.Sort => new PlantSort { Name = "Пустой", Producer = await db.Producers.SingleAsync() },
                _ => new Producer { Name = "Не используется" }
            };
            db.Add(entry); await db.SaveChangesAsync(); return entry.Id;
        });
        Assert.True((await service.DeleteEntryAsync(kind, unusedId)).IsSuccess);
        Assert.True((await service.DeleteEntryAsync(kind, unusedId)).IsFailure);
        Assert.True((await service.DeleteEntryAsync(kind, 0)).IsFailure);
    }
}

