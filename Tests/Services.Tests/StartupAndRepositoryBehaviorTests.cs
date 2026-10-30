using Bonfire.Domain.Functional;
using Bonfire.Data;
using BonfireDB.Context;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Services.Tests;

public class StartupAndRepositoryBehaviorTests
{
    [Fact]
    public async Task Initializer_MigratesLegacyMetadataOnceWithoutOverwritingCurrentValues()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new DbBonfire(new DbContextOptionsBuilder<DbBonfire>().UseSqlite(connection).Options);
        var initializer = new DbInitializer(db, NullLogger<DbInitializer>.Instance);
        await initializer.InitializeAsync();
        var seed = new Seed
        {
            Plant = new Plant
            {
                PlantCulture = new PlantCulture { Name = "Томат", Class = "Овощи" },
                PlantSort = new PlantSort { Name = "Черри", Producer = new Producer { Name = "Фирма" } }
            },
            SeedsInfo = new SeedsInfo()
        };
        db.Seeds.Add(seed); await db.SaveChangesAsync();
        var date = new DateTime(2026, 3, 1);
        var legacy = new Seedling { Plant = seed.Plant, SeedId = seed.Id, SeedlingInfos = [new() { SeedlingNumber = 0, LandingDate = date, LunarPhase = "Растущий серп", SeedlingSource = "Из семян", PlantPlace = "Окно" }] };
        var current = new Seedling { Plant = seed.Plant, SeedId = seed.Id, LandingDate = date.AddDays(1), PlantPlace = "Теплица", SeedlingInfos = [new() { SeedlingNumber = 0, LandingDate = date, PlantPlace = "Старое" }] };
        var missingMeta = new Seedling { Plant = seed.Plant, SeedId = seed.Id };
        db.Seedlings.AddRange(legacy, current, missingMeta); await db.SaveChangesAsync();
        await initializer.InitializeAsync();
        Assert.Equal(date, legacy.LandingDate);
        Assert.Equal("Растущий серп", legacy.LunarPhase);
        Assert.Equal("Из семян", legacy.SeedlingSource);
        Assert.Equal("Окно", legacy.PlantPlace);
        Assert.Equal("Теплица", current.PlantPlace);
        Assert.Null(missingMeta.LandingDate);
        legacy.SeedlingInfos[0].PlantPlace = "Не копировать повторно";
        await db.SaveChangesAsync();
        await initializer.InitializeAsync();
        Assert.Equal("Окно", legacy.PlantPlace);
    }

    [Fact]
    public async Task GreenhouseRepository_ReloadsParentChildrenAndPlantingGridAcrossContexts()
    {
        using var fx = new SqliteUowFixture();
        var service = fx.CreateGardenService();
        var plan = await fx.PreparePlanAsync("План", 2026);
        var garden = (await service.CreateGardenFromRequestAsync(new(plan.Id, "Участок", 20, 15, 150))).ValueOrThrow();
        var greenhouse = (await service.CreateGreenhouseFromRequestAsync(new(garden.Id, "Теплица", 6, 3, 150, 0, 0))).ValueOrThrow().Properties;
        var inner = (await service.CreateGreenhouseFromRequestAsync(new(greenhouse.Id, "Внутренняя", 2, 1, 150, 0, 0))).ValueOrThrow().Properties;
        var bed = (await service.CreateElementFromRequestAsync(new(greenhouse.Id, "Грядка", new Bonfire.Domain.Garden.GardenElementKind.Bed(), 450, 0, 100, 100))).ValueOrThrow().Properties;
        (await service.ResizeGridAsync(new(bed.Id, 2, 3))).ValueOrThrow();
        var loaded = await fx.LoadAsync<Greenhouse>(greenhouse.Id);
        Assert.Equal(garden.Id, loaded!.ParentPlot.Id);
        Assert.Equal(inner.Id, Assert.Single(loaded.Greenhouses).Id);
        Assert.Equal(6, Assert.Single(loaded.Elements).PlantingSpots.Count);
        Assert.Equal(greenhouse.Id, (await fx.LoadAsync<GardenElement>(bed.Id))!.Plot.Id);
    }

    [Fact]
    public async Task SeedlingInfoRepository_ReloadsTreatmentsAndReplantings()
    {
        using var fx = new SqliteUowFixture();
        var id = await fx.QueryAsync(async db =>
        {
            var info = new SeedlingInfo { Replants = [new() { ReplantingDate = new DateTime(2026, 4, 1) }], Treatments = [new()] };
            db.SeedlingInfos.Add(info); await db.SaveChangesAsync();
            return info.Id;
        });
        await using var uow = fx.Factory.Create();
        var loaded = await uow.Repository<SeedlingInfo>().GetAsync(id);
        Assert.Equal(new DateTime(2026, 4, 1), Assert.Single(loaded!.Replants!).ReplantingDate);
        Assert.Single(loaded.Treatments!);
    }
}
