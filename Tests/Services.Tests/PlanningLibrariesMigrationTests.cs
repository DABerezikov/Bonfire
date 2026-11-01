using BonfireDB.Context;
using BonfireDB.Entities.Planning;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Services.Tests;

public class PlanningLibrariesMigrationTests
{
    [Fact]
    public async Task Migration_UpgradeRetryAndRollback_PreserveExistingInventoryAndEditedRegion()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new DbBonfire(new DbContextOptionsBuilder<DbBonfire>().UseSqlite(connection).Options);
        var migrations = db.Database.GetMigrations().ToArray();
        var index = Array.FindIndex(migrations, x => x.EndsWith("AddPlanningLibraries", StringComparison.Ordinal));
        Assert.True(index > 0);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(migrations[index - 1]);
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Producers (Name) VALUES ('Фирма')");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO PlantsSort (Name, ProducerId, MinGerminationTime, YieldKg) VALUES ('Сорт', 1, 8, 3.5)");
        await migrator.MigrateAsync();
        Assert.False(db.Database.HasPendingModelChanges());
        var region = await db.Set<PlanningRegionRow>().SingleAsync();
        Assert.Equal("tomsk", region.StableKey);
        region.Name = "Свой Томск";
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        await migrator.MigrateAsync();
        Assert.Equal("Свой Томск", (await db.Set<PlanningRegionRow>().SingleAsync()).Name);
        var sort = await db.PlantsSort.SingleAsync();
        Assert.Equal(8, sort.MinGerminationTime);
        Assert.Equal(3.5, sort.YieldKg);
        db.ChangeTracker.Clear();
        await migrator.MigrateAsync(migrations[index - 1]);
        sort = await db.PlantsSort.SingleAsync();
        Assert.Equal(8, sort.MinGerminationTime);
        Assert.Equal(3.5, sort.YieldKg);
        await migrator.MigrateAsync();
        Assert.Equal("Томск", (await db.Set<PlanningRegionRow>().SingleAsync()).Name);
    }
}
