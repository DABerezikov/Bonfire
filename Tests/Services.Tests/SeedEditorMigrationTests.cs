using BonfireDB.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Services.Tests;

public class SeedEditorMigrationTests
{
    [Fact]
    public async Task Migrate_ExistingDatabase_PreservesCharacteristicsAndCollectsColors()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DbBonfire>().UseSqlite(connection).Options;
        await using var db = new DbBonfire(options);
        var migrations = db.Database.GetMigrations().ToArray();
        var editorMigration = Array.FindIndex(migrations, migration => migration.EndsWith("SeedSortEditor", StringComparison.Ordinal));
        Assert.True(editorMigration > 0);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(migrations[editorMigration - 1]);
        // Старая схема ещё не содержит новых столбцов: вставляем исходные данные SQL-командой.
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Producers (Name) VALUES ('Проверка')");
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO PlantsSort (Name, ProducerId, MinGerminationTime, MaxGerminationTime,
                AgeOfSeedlings, GrowingSeason, LandingPattern, PlantHeight, PlantColor, Description)
            VALUES ('Старый сорт', 1, 70, 90, 40, 100, 30, 80, 'Красный', 'Исходное описание')
            """);
        await migrator.MigrateAsync();
        Assert.False(db.Database.HasPendingModelChanges());

        var sort = await db.PlantsSort.SingleAsync();
        Assert.Equal(70, sort.MinGerminationTime);
        Assert.Equal(90, sort.MaxGerminationTime);
        Assert.Equal(40, sort.AgeOfSeedlings);
        Assert.Equal(100, sort.GrowingSeason);
        Assert.Equal(30, sort.LandingPattern);
        Assert.Equal(80, sort.PlantHeight);
        Assert.Equal("Исходное описание", sort.Description);
        Assert.Null(sort.MaxPlantHeight);
        Assert.Null(sort.MaxAgeOfSeedlings);
        Assert.Null(sort.Maturity);
        Assert.Null(sort.MinFruitSize);
        Assert.Null(sort.MaxFruitSize);
        Assert.Contains(await db.SortOptions.ToListAsync(), o => o.Category == "Цвет" && o.Name == "Красный");
    }

    [Fact]
    public async Task FruitSizeMigration_UpgradeAndRollback_PreserveExistingSort()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DbBonfire>().UseSqlite(connection).Options;
        await using var db = new DbBonfire(options);
        var migrations = db.Database.GetMigrations().ToArray();
        Assert.EndsWith("AddFruitSize", migrations[^1]);
        var previous = migrations[^2];
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(previous);
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Producers (Name) VALUES ('Фирма')");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO PlantsSort (Name, ProducerId, MinFruitWeight) VALUES ('Старый сорт', 1, 10)");
        await migrator.MigrateAsync();
        Assert.False(db.Database.HasPendingModelChanges());
        var sort = await db.PlantsSort.SingleAsync();
        Assert.Equal("Старый сорт", sort.Name);
        Assert.Equal(10, sort.MinFruitWeight);
        Assert.Null(sort.MinFruitSize);
        Assert.Null(sort.MaxFruitSize);
        sort.MinFruitSize = 0;
        sort.MaxFruitSize = 5;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        sort = await db.PlantsSort.SingleAsync();
        Assert.Equal(0, sort.MinFruitSize);
        Assert.Equal(5, sort.MaxFruitSize);
        db.ChangeTracker.Clear();
        await migrator.MigrateAsync(previous);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT Name, MinFruitWeight FROM PlantsSort";
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("Старый сорт", reader.GetString(0));
            Assert.Equal(10, reader.GetDouble(1));
        }
        await migrator.MigrateAsync();
        sort = await db.PlantsSort.SingleAsync();
        Assert.Equal("Старый сорт", sort.Name);
        Assert.Null(sort.MinFruitSize);
        Assert.Null(sort.MaxFruitSize);
    }
}
