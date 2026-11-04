using BonfireDB.Context;
using BonfireDB.Entities.Planning;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Services.Tests;

public class PlantingPlanMigrationTests
{
    [Fact]
    public async Task Migration_UpgradeAndRollback_PreservesLibrariesAndExistingFacts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new DbBonfire(new DbContextOptionsBuilder<DbBonfire>().UseSqlite(connection).Options);
        var migrations = db.Database.GetMigrations().ToArray();
        var index = Array.FindIndex(migrations, x => x.EndsWith("AddSeasonalPlantingPlans", StringComparison.Ordinal));
        Assert.True(index > 0);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(migrations[index - 1]);
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Producers (Name) VALUES ('Сохранить')");
        await db.Database.ExecuteSqlRawAsync("UPDATE PlanningRegions SET Name = 'Свой Томск' WHERE Id = 1");
        await migrator.MigrateAsync();
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(0, await db.Set<SeasonalPlantingPlanRow>().CountAsync());
        await migrator.MigrateAsync();
        Assert.Equal("Свой Томск", (await db.Set<PlanningRegionRow>().SingleAsync()).Name);
        await migrator.MigrateAsync(migrations[index - 1]);
        Assert.Equal("Сохранить", (await db.Producers.SingleAsync()).Name);
        Assert.Equal("Свой Томск", (await db.Set<PlanningRegionRow>().SingleAsync()).Name);
        await migrator.MigrateAsync();
        Assert.Equal(0, await db.Set<PlannedBatchRow>().CountAsync());
    }
}
