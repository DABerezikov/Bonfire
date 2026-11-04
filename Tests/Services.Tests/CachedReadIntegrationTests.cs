using Bonfire.Application.Caching;
using Bonfire.Application.Seeds;
using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;
using Bonfire.Models;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class CachedReadIntegrationTests
{
    [Fact]
    public async Task AddingUnusedColorRefreshesOptionsWithoutReloadingInventory()
    {
        var commands = new List<string>(); using var fixture = new SqliteUowFixture(commands.Add);
        var seeds = fixture.CreateSeedsService();
        await seeds.AddOrUpdateSeedAsync(new("Томат", "Черри", "Фирма", "Овощи", "Куплено", Units.PiecesOption,
            10, 1, 0, new DateTime(2028,12,31), ""));
        await seeds.ReadSeedsAsync(); await seeds.GetEditorOptionsAsync();
        var inventory = seeds.InventoryVersion; var options = seeds.OptionsVersion;
        var manage = new Bonfire.Application.Libraries.ManageSortOptions(new BonfireDB.Libraries.DbSortOptionStore(fixture.Factory), fixture.Cache);
        Assert.True((await manage.SaveAsync(Bonfire.Domain.Libraries.SortOptionCategory.Color, Option.None<string>(), "Оранжевый")).IsSuccess);
        Assert.Equal(inventory, seeds.InventoryVersion); Assert.True(seeds.OptionsVersion > options);
        commands.Clear(); await seeds.ReadSeedsAsync(); Assert.Empty(commands);
        Assert.Contains("Оранжевый", (await seeds.GetEditorOptionsAsync()).ValueOrThrow().Colors);
    }

    [Fact]
    public async Task WriterChecksFreshStockEvenWhenReadSnapshotIsCached()
    {
        using var fixture = new SqliteUowFixture(); var seeds = fixture.CreateSeedsService();
        var acquired = (await seeds.AddOrUpdateSeedAsync(new("Томат", "Черри", "Фирма", "Овощи", "Куплено", Units.PiecesOption,
            10, 1, 0, new DateTime(2028,12,31), ""))).ValueOrThrow();
        await seeds.ReadSeedsAsync();
        // Имитация изменения вне приложения: снимок остаётся старым, writer обязан читать свежий остаток.
        await fixture.ExecuteAsync(async db => { (await db.SeedsInfo.SingleAsync()).AmountSeeds = 2; await db.SaveChangesAsync(); });
        Assert.Equal(10, Assert.Single((await seeds.ReadSeedsAsync()).ValueOrThrow()).SeedsInfo.AmountSeeds);
        var result = await fixture.CreateSeedlingsService().SowAsync(new SowingRequest(acquired.SeedId, 3, SeedUnit.Pieces,
            new DateTime(2027,3,1), "Посеяно", Option.None<string>()));
        Assert.True(result.IsFailure);
        Assert.Equal(2, await fixture.QueryAsync(db => db.SeedsInfo.Select(info => info.AmountSeeds).SingleAsync()));
    }

    [Fact]
    public async Task FailedCommitDoesNotInvalidateSuccessfulReadCache()
    {
        using var fixture = new SqliteUowFixture();
        await fixture.ExecuteAsync(async db => { db.Add(new SortOption { Category = "Цвет", Name = "Белый" }); await db.SaveChangesAsync(); });
        var version = fixture.Cache.Version(ReadArea.SortOptions);
        await using var unit = fixture.Factory.Create();
        unit.Repository<SortOption>().Add(new() { Category = "Цвет", Name = "Белый" });
        await Assert.ThrowsAsync<DbUpdateException>(() => unit.SaveChangesAsync());
        Assert.Equal(version, fixture.Cache.Version(ReadArea.SortOptions));
    }

    [Fact]
    public async Task RepeatedReadsIssueNoSql_AndSowingInvalidatesOnlyStockAndSeedlings()
    {
        var commands = new List<string>(); using var fixture = new SqliteUowFixture(commands.Add);
        var seeds = fixture.CreateSeedsService();
        var receipt = (await seeds.AddOrUpdateSeedAsync(new("Томат", "Черри", "Фирма", "Овощи", "Куплено", Units.PiecesOption,
            10, 1, 0, new DateTime(2028,12,31), ""))).ValueOrThrow();
        var initial = (await seeds.ReadSeedsAsync()).ValueOrThrow();
        await seeds.GetEditorOptionsAsync();
        var seedlings = fixture.CreateSeedlingsService(); await seedlings.ReadSeedlingsAsync();
        commands.Clear();
        await fixture.CreateSeedsService().ReadSeedsAsync(); await seeds.GetEditorOptionsAsync(); await seedlings.ReadSeedlingsAsync();
        Assert.Empty(commands);
        var catalogVersion = seeds.OptionsVersion;
        Assert.True((await seedlings.SowAsync(new SowingRequest(receipt.SeedId, 3, SeedUnit.Pieces,
            new DateTime(2027,3,1), "Посеяно", Option.None<string>()))).IsSuccess);
        Assert.Equal(catalogVersion, seeds.OptionsVersion);
        commands.Clear(); await seeds.GetEditorOptionsAsync(); Assert.Empty(commands);
        Assert.Equal(7, Assert.Single((await seeds.ReadSeedsAsync()).ValueOrThrow()).SeedsInfo.AmountSeeds);
        Assert.Equal(10, Assert.Single(initial).SeedsInfo.AmountSeeds);
        Assert.NotEmpty(commands);
        Assert.Single((await seedlings.ReadSeedlingsAsync()).ValueOrThrow());
        commands.Clear(); await seeds.ReadSeedsAsync(); await seedlings.ReadSeedlingsAsync(); Assert.Empty(commands);
    }

    [Fact]
    public async Task RenameAndDeletionInvalidateRelatedSnapshots_AndRejectedWriteKeepsCache()
    {
        var commands = new List<string>(); using var fixture = new SqliteUowFixture(commands.Add);
        var seeds = fixture.CreateSeedsService();
        var acquired = (await seeds.AddOrUpdateSeedAsync(new("Томат", "Черри", "Фирма", "Овощи", "Куплено", Units.PiecesOption,
            10, 1, 0, new DateTime(2028,12,31), ""))).ValueOrThrow();
        var original = Assert.Single((await seeds.ReadSeedsAsync()).ValueOrThrow());
        await seeds.GetEditorOptionsAsync();
        var library = fixture.CreateLibraryService();
        Assert.True((await library.UpdateCultureAsync(new CultureEditModel { Id = original.Plant.PlantCulture.Id, Name = "Помидор", Class = "Овощи" })).IsSuccess);
        Assert.Equal("Помидор", Assert.Single((await seeds.ReadSeedsAsync()).ValueOrThrow()).Plant.PlantCulture.Name);
        var version = seeds.InventoryVersion;
        Assert.True((await library.DeleteEntryAsync(Bonfire.Application.Libraries.LibraryEntryKind.Culture, original.Plant.PlantCulture.Id)).IsFailure);
        Assert.Equal(version, seeds.InventoryVersion);
        commands.Clear(); await seeds.ReadSeedsAsync(); Assert.Empty(commands);
        Assert.True((await seeds.DeleteSeedWithReasonAsync(acquired.SeedId, "Удалено")).IsSuccess);
        Assert.Empty((await seeds.ReadSeedsAsync()).ValueOrThrow());
    }

    [Fact]
    public async Task OwnedProfileChangeInvalidatesPlanningLibrariesWithoutInvalidatingStock()
    {
        using var fixture = new SqliteUowFixture();
        var id = await fixture.QueryAsync(async db =>
        {
            var row = new BonfireDB.Entities.Planning.PlanningRegionRow { Name = "Регион", TimeZoneId = "UTC", LastSpringFrost = new() { StartMonth = 5, StartDay = 1, EndMonth = 5, EndDay = 10 } };
            db.Add(row); await db.SaveChangesAsync(); return row.Id;
        });
        var version = fixture.Cache.Version(ReadArea.PlanningLibraries);
        await using (var unit = fixture.Factory.Create())
        {
            var row = await unit.Repository<BonfireDB.Entities.Planning.PlanningRegionRow>().GetAsync(id);
            row!.LastSpringFrost!.EndDay = 15; await unit.SaveChangesAsync();
        }
        Assert.True(fixture.Cache.Version(ReadArea.PlanningLibraries) > version);
        Assert.Equal(0, fixture.Cache.Version(ReadArea.Seeds));
    }
}
