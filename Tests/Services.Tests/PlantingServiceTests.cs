using Bonfire.Services.Interfaces;
using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Entities.GardenPlanning.SpotStates;
using Microsoft.EntityFrameworkCore;
using MoonCalendar;

namespace Services.Tests;

// Интеграционные тесты PlantingService на реальном SQLite + UoW.
// PlantAsync выполняется в одном UoW (один контекст, один SaveChanges).
public class PlantingServiceTests : IDisposable
{
    private readonly SqliteUowFixture _fx = new();

    public void Dispose() => _fx.Dispose();

    private PlantingService CreateService() => new(_fx.Factory, new MoonPhase());

    private static readonly DateTime PlantedDate = new(2026, 5, 1);

    private static Plant MakePlant() => new()
    {
        PlantCulture = new PlantCulture { Name = "Огурец" },
        PlantSort = new PlantSort { Name = "Кураж", Producer = new Producer { Name = "Гавриш" } }
    };

    private async Task<int> SeedSpotAsync(string state = nameof(EmptySpotState))
        => await _fx.QueryAsync(async db =>
        {
            var plan = new GardenPlan { Name = "План", Year = 2026 };
            db.GardenPlans.Add(plan);
            await db.SaveChangesAsync();

            var garden = new Garden
            {
                GardenPlanId = plan.Id, Name = "Участок",
                WidthMeters = 10, HeightMeters = 5, CanvasWidth = 400, CanvasHeight = 200
            };
            db.Gardens.Add(garden);
            await db.SaveChangesAsync();

            var bed = new Bed { PlotId = garden.Id, Name = "Грядка" };
            var spot = new PlantingSpot { Row = 0, Column = 0, StateTypeName = state };
            bed.PlantingSpots.Add(spot);
            db.GardenElements.Add(bed);
            await db.SaveChangesAsync();
            return spot.Id;
        });

    private async Task<int> SeedSeedlingAsync(double quantity = 10, double weight = 0, int germinated = 0)
        => await _fx.QueryAsync(async db =>
        {
            var seedling = new Seedling { Plant = MakePlant(), Quantity = quantity, Weight = weight };
            for (var i = 0; i < germinated; i++)
                seedling.SeedlingInfos.Add(new SeedlingInfo { GerminationDate = DateTime.Today, SeedlingNumber = i + 1 });
            db.Seedlings.Add(seedling);
            await db.SaveChangesAsync();
            return seedling.Id;
        });

    private async Task<int> SeedSeedAsync(double amountSeeds = 20, double? amountWeight = null)
        => await _fx.QueryAsync(async db =>
        {
            var info = new SeedsInfo { AmountSeeds = amountSeeds, AmountSeedsWeight = amountWeight, ExpirationDate = DateTime.Today.AddYears(1) };
            var seed = new Seed { Plant = MakePlant(), SeedsInfo = info };
            info.Seed = seed;
            db.Seeds.Add(seed);
            await db.SaveChangesAsync();
            return seed.Id;
        });

    private static PlantRequest SeedlingRequest(int spotId, int entityId, bool weight = false, double amount = 3) =>
        new(spotId, PlantSourceKind.Seedling, entityId, weight, amount, PlantedDate, "Огурец Кураж", "План / Участок / Грядка");

    private static PlantRequest SeedRequest(int spotId, int entityId, bool weight = false, double amount = 5) =>
        new(spotId, PlantSourceKind.Seed, entityId, weight, amount, PlantedDate, "Огурец Кураж", "План / Участок / Грядка");

    // ── Ошибки ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlantAsync_SpotNotFound_ReturnsFailure()
    {
        var seedlingId = await SeedSeedlingAsync();

        var result = await CreateService().PlantAsync(SeedlingRequest(99999, seedlingId));

        Assert.False(result.Success);
        Assert.Null(result.SeedlingInfoId);
    }

    [Fact]
    public async Task PlantAsync_InvalidTransition_ReturnsFailureAndDoesNotChangeState()
    {
        var spotId = await SeedSpotAsync(nameof(PlantedSpotState)); // Planted → Planted запрещён
        var seedlingId = await SeedSeedlingAsync();

        var result = await CreateService().PlantAsync(SeedlingRequest(spotId, seedlingId));

        Assert.False(result.Success);
        var seedling = await _fx.QueryAsync(db => db.Seedlings.FirstAsync(s => s.Id == seedlingId));
        Assert.Equal(10, seedling.Quantity); // инвентарь не тронут
    }

    [Fact]
    public async Task PlantAsync_FromSeedling_SeedlingNotFound_ReturnsFailure()
    {
        var spotId = await SeedSpotAsync();

        var result = await CreateService().PlantAsync(SeedlingRequest(spotId, 99999));

        Assert.False(result.Success);
        var spot = await _fx.QueryAsync(db => db.PlantingSpots.FirstAsync(s => s.Id == spotId));
        Assert.Equal(nameof(EmptySpotState), spot.StateTypeName); // ячейка не изменена
    }

    [Fact]
    public async Task PlantAsync_FromSeed_SeedNotFound_ReturnsFailure()
    {
        var spotId = await SeedSpotAsync();

        var result = await CreateService().PlantAsync(SeedRequest(spotId, 99999));

        Assert.False(result.Success);
    }

    // ── Посадка из рассады ────────────────────────────────────────────────────

    [Fact]
    public async Task PlantAsync_FromSeedling_ReducesSourceQuantity()
    {
        var spotId = await SeedSpotAsync();
        var seedlingId = await SeedSeedlingAsync(quantity: 10);

        var result = await CreateService().PlantAsync(SeedlingRequest(spotId, seedlingId, amount: 3));

        Assert.True(result.Success);
        Assert.NotNull(result.SeedlingInfoId);

        var seedling = await _fx.QueryAsync(db => db.Seedlings.FirstAsync(s => s.Id == seedlingId));
        Assert.Equal(7, seedling.Quantity); // 10 − 3 = 7

        var spot = await _fx.QueryAsync(db => db.PlantingSpots.FirstAsync(s => s.Id == spotId));
        Assert.Equal(nameof(PlantedSpotState), spot.StateTypeName);
        Assert.Equal(result.SeedlingInfoId, spot.SeedlingInfoId);
        Assert.Equal("Огурец Кураж", spot.Note);
    }

    [Fact]
    public async Task PlantAsync_FromSeedling_DeductsWeight()
    {
        var spotId = await SeedSpotAsync();
        var seedlingId = await SeedSeedlingAsync(quantity: 0, weight: 10.0);

        var result = await CreateService().PlantAsync(SeedlingRequest(spotId, seedlingId, weight: true, amount: 3));

        Assert.True(result.Success);
        var seedling = await _fx.QueryAsync(db => db.Seedlings.FirstAsync(s => s.Id == seedlingId));
        Assert.Equal(7.0, seedling.Weight); // 10.0 − 3.0 = 7.0
    }

    // ── Посадка из семян ──────────────────────────────────────────────────────

    [Fact]
    public async Task PlantAsync_FromSeed_CreatesSeedlingDeductsSeedsAndReusesExistingPlant()
    {
        var spotId = await SeedSpotAsync();
        var seedId = await SeedSeedAsync(amountSeeds: 20);

        var result = await CreateService().PlantAsync(SeedRequest(spotId, seedId, amount: 5));

        Assert.True(result.Success);
        Assert.NotNull(result.SeedlingInfoId);

        var seed = await _fx.QueryAsync(db => db.Seeds.Include(s => s.SeedsInfo).FirstAsync(s => s.Id == seedId));
        Assert.Equal(15, seed.SeedsInfo.AmountSeeds);

        // Создана ровно одна рассада, существующее растение НЕ продублировано.
        var seedlingCount = await _fx.QueryAsync(db => db.Seedlings.CountAsync());
        Assert.Equal(1, seedlingCount);
        var plantCount = await _fx.QueryAsync(db => db.Plants.CountAsync());
        Assert.Equal(1, plantCount);

        var spot = await _fx.QueryAsync(db => db.PlantingSpots.FirstAsync(s => s.Id == spotId));
        Assert.Equal(nameof(PlantedSpotState), spot.StateTypeName);
        Assert.Equal(result.SeedlingInfoId, spot.SeedlingInfoId);
    }

    [Fact]
    public async Task PlantAsync_FromSeed_DeductsWeight()
    {
        var spotId = await SeedSpotAsync();
        var seedId = await SeedSeedAsync(amountSeeds: 0, amountWeight: 10.0);

        await CreateService().PlantAsync(SeedRequest(spotId, seedId, weight: true, amount: 4));

        var seed = await _fx.QueryAsync(db => db.Seeds.Include(s => s.SeedsInfo).FirstAsync(s => s.Id == seedId));
        Assert.Equal(6.0, seed.SeedsInfo.AmountSeedsWeight);
    }

    // ── Нельзя посадить больше, чем есть ──────────────────────────────────────

    [Fact]
    public async Task PlantAsync_FromSeed_AmountExceedsAvailable_ClampsToAvailable()
    {
        var spotId = await SeedSpotAsync();
        var seedId = await SeedSeedAsync(amountSeeds: 2);

        var result = await CreateService().PlantAsync(SeedRequest(spotId, seedId, amount: 5));

        Assert.True(result.Success);
        var seed = await _fx.QueryAsync(db => db.Seeds.Include(s => s.SeedsInfo).FirstAsync(s => s.Id == seedId));
        Assert.Equal(0, seed.SeedsInfo.AmountSeeds);              // остаток не ушёл в минус
        var seedling = await _fx.QueryAsync(db => db.Seedlings.FirstAsync());
        Assert.Equal(2, seedling.Quantity);                      // рассада создана ровно на доступные 2, не 5
    }

    [Fact]
    public async Task PlantAsync_FromSeedling_EmptyStock_ReturnsFailureAndKeepsSpotEmpty()
    {
        var spotId = await SeedSpotAsync();
        var seedlingId = await SeedSeedlingAsync(quantity: 0);

        var result = await CreateService().PlantAsync(SeedlingRequest(spotId, seedlingId, amount: 3));

        Assert.False(result.Success);
        var spot = await _fx.QueryAsync(db => db.PlantingSpots.FirstAsync(s => s.Id == spotId));
        Assert.Equal(nameof(EmptySpotState), spot.StateTypeName);
        Assert.Equal(0, await _fx.QueryAsync(db => db.SeedlingInfos.CountAsync()));
    }
}
