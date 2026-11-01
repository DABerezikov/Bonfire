using System.Text.Json;
using BonfireDB.Context;
using BonfireDB.Entities;
using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Entities.Planning;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Services.Tests;

public class PlanningFinalMigrationTests
{
    [Fact]
    public async Task AllPlanningMigrations_PreserveLegacyFactsHistoryGeometryAndJournalOnFkRebuild()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new DbBonfire(new DbContextOptionsBuilder<DbBonfire>().UseSqlite(connection).Options);
        var migrations = db.Database.GetMigrations().ToArray(); var migrator = db.GetService<IMigrator>();
        var first = Array.FindIndex(migrations, x => x.EndsWith("AddPlanningLibraries", StringComparison.Ordinal));
        var last = Array.FindIndex(migrations, x => x.EndsWith("AddPerennialPlanning", StringComparison.Ordinal));
        Assert.True(first > 0 && last > first);
        await migrator.MigrateAsync(migrations[first - 1]);
        var plant = new Plant { PlantCulture = new() { Name = "Историческая культура", Class = "Овощи" },
            PlantSort = new() { Name = "Исторический сорт", Producer = new() { Name = "Производитель" }, YieldKg = 3.5, MinGerminationTime = 8 } };
        var seed = new Seed { Plant = plant, SeedsInfo = new() { AmountSeeds = 100, AmountSeedsWeight = null, Note = "Пакет до миграции" } };
        db.Add(seed); await db.SaveChangesAsync();
        var info = new SeedlingInfo { SeedlingNumber = 1, LandingDate = new(2025, 3, 1), GerminationDate = new(2025, 3, 10),
            LunarPhase = "Историческая фаза", Note = "Рассада до миграции", IsDead = false,
            Replants = [new() { ReplantingDate = new(2025, 4, 1), PotVolume = .5, ReplantingNote = "История пересадки" }],
            Treatments = [new() { TreatmentDate = new(2025, 4, 5), Product = "Препарат", TreatmentMethod = "Обработка" }] };
        db.Add(new Seedling { Plant = plant, SeedId = seed.Id, Quantity = 10, PlantedOut = 1, SeedlingInfos = [info], LunarPhase = "Историческая фаза" });
        var bed = new Bed { Name = "Грядка", Plot = new Garden { Name = "Участок", GardenPlan = new() { Name = "Старый план", Year = 2025 },
            WidthMeters = 6, HeightMeters = 3, CanvasWidth = 900, CanvasHeight = 450 }, DisplayWidth = 300, DisplayHeight = 135,
            X = 15, Y = 30, GridRows = 2, GridColumns = 3, IsLocked = true, Note = "Старая геометрия" };
        for (var row = 0; row < 2; row++) for (var col = 0; col < 3; col++)
            bed.PlantingSpots.Add(new() { Row = row, Column = col, Note = $"Ячейка {row}/{col}",
                SeedlingInfo = row == 0 && col == 0 ? info : null, HarvestDate = row == 0 && col == 0 ? new DateTime(2025, 8, 1) : null });
        db.Add(bed); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var before = await Facts(db);
        await migrator.MigrateAsync(); Assert.False(db.Database.HasPendingModelChanges()); Assert.Equal(before, await Facts(db));
        db.Add(new HarvestEntryRow { Version = 1, Date = new(2025, 8, 1), MassKg = 1.25m, Note = "", SeasonYear = 2025,
            RegionName = "Томск", TimeZoneId = "Asia/Tomsk", SnapshotCultureId = plant.PlantCulture.Id, SnapshotSortId = plant.PlantSort.Id,
            CultureId = plant.PlantCulture.Id, SortId = plant.PlantSort.Id, CultureName = plant.PlantCulture.Name, SortName = plant.PlantSort.Name });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        await migrator.MigrateAsync(migrations[last - 1]);
        var journal = await db.Set<HarvestEntryRow>().AsNoTracking().SingleAsync(); Assert.Equal(1.25m, journal.MassKg); Assert.Equal("", journal.Note);
        Assert.Equal(before, await Facts(db));
        await migrator.MigrateAsync(); Assert.Equal(1.25m, (await db.Set<HarvestEntryRow>().AsNoTracking().SingleAsync()).MassKg);
        await migrator.MigrateAsync(migrations[first - 1]); Assert.Equal(before, await Facts(db));
        await migrator.MigrateAsync(); Assert.Equal(before, await Facts(db)); Assert.Empty(await db.Set<HarvestEntryRow>().ToArrayAsync());
        await using var check = connection.CreateCommand(); check.CommandText = "PRAGMA foreign_key_check";
        await using var reader = await check.ExecuteReaderAsync(); Assert.False(await reader.ReadAsync());
    }

    private static async Task<string> Facts(DbBonfire db) => JsonSerializer.Serialize(new
    {
        Seeds = await db.Set<Seed>().AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, PlantId = x.Plant.Id, x.SeedsInfo.AmountSeeds, x.SeedsInfo.AmountSeedsWeight, x.SeedsInfo.Note }).ToArrayAsync(),
        Sorts = await db.PlantsSort.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.Name, x.YieldKg, x.MinGerminationTime }).ToArrayAsync(),
        Seedlings = await db.Set<Seedling>().AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.SeedId, x.Quantity, x.PlantedOut, x.LunarPhase }).ToArrayAsync(),
        Infos = await db.Set<SeedlingInfo>().AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.LandingDate, x.GerminationDate, x.LunarPhase, x.Note, x.IsDead }).ToArrayAsync(),
        Replants = await db.Set<Replanting>().AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.ReplantingDate, x.PotVolume, x.ReplantingNote }).ToArrayAsync(),
        Treatments = await db.Set<Treatment>().AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.TreatmentDate, x.Product, x.TreatmentMethod }).ToArrayAsync(),
        Plots = await db.Set<GardenPlot>().AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.Name, x.WidthMeters, x.HeightMeters, x.CanvasWidth, x.CanvasHeight }).ToArrayAsync(),
        Beds = await db.Set<GardenElement>().AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.PlotId, x.Name, x.X, x.Y, x.DisplayWidth, x.DisplayHeight, x.GridRows, x.GridColumns, x.IsLocked, x.Note }).ToArrayAsync(),
        Spots = await db.Set<PlantingSpot>().AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.Row, x.Column, x.GardenElementId, x.SeedlingInfoId, x.StateTypeName, x.HarvestDate, x.Note }).ToArrayAsync()
    });
}
