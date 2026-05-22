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
    public async Task PlantAsync_FromGerminatedSeedling_ReducesSourceAndCreatesPlantedBatch()
    {
        var spotId = await SeedSpotAsync();
        var seedlingId = await SeedSeedlingAsync(germinated: 3); // 3 взошедших живых

        var result = await CreateService().PlantAsync(SeedlingRequest(spotId, seedlingId, amount: 2));

        Assert.True(result.Success);
        Assert.NotNull(result.SeedlingInfoId);

        // Источник: высажено 2 → Осталось = 3 − 2 = 1.
        var source = await _fx.QueryAsync(db => db.Seedlings.FirstAsync(s => s.Id == seedlingId));
        Assert.Equal(2, source.PlantedOut);

        // Отдельная «высаженная партия» — новая Seedling с количеством 2, привязанная к грядке.
        var batch = await _fx.QueryAsync(db => db.Seedlings.FirstOrDefaultAsync(s => s.Id != seedlingId));
        Assert.NotNull(batch);
        Assert.Equal(2, batch!.Quantity);
        Assert.Equal("План / Участок / Грядка", batch.PlantPlace);
        Assert.True(batch.IsPlantedInBed); // партия помечена как уже в грядке
        Assert.Equal(2, await _fx.QueryAsync(db => db.Seedlings.CountAsync())); // источник + партия
        Assert.Equal(1, await _fx.QueryAsync(db => db.Plants.CountAsync()));     // растение не продублировано

        var spot = await _fx.QueryAsync(db => db.PlantingSpots.FirstAsync(s => s.Id == spotId));
        Assert.Equal(nameof(PlantedSpotState), spot.StateTypeName);
        Assert.Equal(result.SeedlingInfoId, spot.SeedlingInfoId);
        Assert.Equal("Огурец Кураж", spot.Note);

        var info = await _fx.QueryAsync(db => db.SeedlingInfos.FirstAsync(i => i.Id == result.SeedlingInfoId));
        Assert.Equal("План / Участок / Грядка", info.PlantPlace);
    }

    [Fact]
    public async Task PlantAsync_FromSeedling_NotGerminated_ReturnsFailure()
    {
        var spotId = await SeedSpotAsync();
        var seedlingId = await SeedSeedlingAsync(quantity: 10, germinated: 0); // посеяно, но не взошло

        var result = await CreateService().PlantAsync(SeedlingRequest(spotId, seedlingId, amount: 3));

        Assert.False(result.Success);
        var spot = await _fx.QueryAsync(db => db.PlantingSpots.FirstAsync(s => s.Id == spotId));
        Assert.Equal(nameof(EmptySpotState), spot.StateTypeName);
    }

    [Fact]
    public async Task PlantAsync_FromSeedling_AmountExceedsGerminated_ClampsToAvailable()
    {
        var spotId = await SeedSpotAsync();
        var seedlingId = await SeedSeedlingAsync(germinated: 2); // только 2 взошедших

        var result = await CreateService().PlantAsync(SeedlingRequest(spotId, seedlingId, amount: 5));

        Assert.True(result.Success);
        var seedling = await _fx.QueryAsync(db => db.Seedlings.FirstAsync(s => s.Id == seedlingId));
        Assert.Equal(2, seedling.PlantedOut); // не больше доступных 2
    }

    [Fact]
    public async Task PlantAsync_FromWeightSownSeedling_PlantsByGerminatedCountNotWeight()
    {
        var spotId = await SeedSpotAsync();
        // Рассада посеяна по весу (3 г), но взошло 4 ростка — сажаем штучно.
        var seedlingId = await SeedSeedlingAsync(quantity: 0, weight: 3.0, germinated: 4);

        var result = await CreateService().PlantAsync(SeedlingRequest(spotId, seedlingId, amount: 2));

        Assert.True(result.Success);
        var source = await _fx.QueryAsync(db => db.Seedlings.FirstAsync(s => s.Id == seedlingId));
        Assert.Equal(2, source.PlantedOut);  // высажено 2 из 4 взошедших
        Assert.Equal(3.0, source.Weight);    // вес источника не списывается
        var batch = await _fx.QueryAsync(db => db.Seedlings.FirstOrDefaultAsync(s => s.Id != seedlingId));
        Assert.Equal(2, batch!.Quantity);    // партия штучная
    }

    [Fact]
    public async Task PlantAsync_PlantedBatchWithGermination_NotRePlantable()
    {
        // Высаживаем из взошедшей рассады → создаётся партия в грядке.
        var spotId = await SeedSpotAsync();
        var seedlingId = await SeedSeedlingAsync(germinated: 3);
        var first = await CreateService().PlantAsync(SeedlingRequest(spotId, seedlingId, amount: 2));
        Assert.True(first.Success);

        // На партии регистрируем всход (рост в грядке) — но повторно сажать её нельзя.
        var batchId = await _fx.QueryAsync(async db =>
        {
            var batch = await db.Seedlings.Include(s => s.SeedlingInfos).FirstAsync(s => s.Id != seedlingId);
            batch.SeedlingInfos.Add(new SeedlingInfo { GerminationDate = DateTime.Today, SeedlingNumber = 1 });
            await db.SaveChangesAsync();
            return batch.Id;
        });

        var spot2 = await SeedSpotAsync();
        var result = await CreateService().PlantAsync(SeedlingRequest(spot2, batchId, amount: 1));

        Assert.False(result.Success); // партия уже в грядке — не доступна к посадке
    }

    [Fact]
    public async Task PlantAsync_WeightSownSeedling_NotGerminated_NotAvailable()
    {
        var spotId = await SeedSpotAsync();
        var seedlingId = await SeedSeedlingAsync(quantity: 0, weight: 3.0, germinated: 0); // посеяно по весу, не взошло

        var result = await CreateService().PlantAsync(SeedlingRequest(spotId, seedlingId, amount: 1));

        Assert.False(result.Success); // без всходов рассада к высадке недоступна
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
