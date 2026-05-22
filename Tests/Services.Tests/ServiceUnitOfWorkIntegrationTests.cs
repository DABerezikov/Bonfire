using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Entities.GardenPlanning.SpotStates;
using Microsoft.EntityFrameworkCore;
using MoonCalendar;

namespace Services.Tests;

// Интеграционные тесты на реальном SQLite + UoW: проверяют рантайм-поведение EF
// при коротком контексте на операцию и графах отсоединённых сущностей —
// именно то, что не ловят тесты на моках.
public class ServiceUnitOfWorkIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fx = new();

    public void Dispose() => _fx.Dispose();

    private SeedsService Seeds() => new(_fx.Factory);
    private SeedlingsService Seedlings() => new(_fx.Factory, new MoonPhase());
    private GardenService Gardens() => new(_fx.Factory);

    private static Plant MakePlant(string culture = "Томат", string sort = "Чёрный принц", string producer = "Гавриш") => new()
    {
        PlantCulture = new PlantCulture { Name = culture, Class = "Овощи" },
        PlantSort = new PlantSort { Name = sort, Producer = new Producer { Name = producer } }
    };

    // ── AddOrUpdateSeedAsync: новый пакет с нуля (полный граф) ─────────────────

    [Fact]
    public async Task AddOrUpdateSeed_BrandNew_PersistsFullGraph()
    {
        var request = new AddSeedRequest(
            "Томат", "Бычье сердце", "Аэлита", "Овощи",
            "Куплено", Units.PiecesOption,
            QuantityInPack: 50, PackCount: 1, CostPack: 0m,
            BestBy: new DateTime(2027, 12, 31), Note: null);

        var (seed, isNew) = await Seeds().AddOrUpdateSeedAsync(request, new List<Seed>());

        Assert.True(isNew);
        Assert.Equal(1, await _fx.QueryAsync(db => db.Seeds.CountAsync()));
        Assert.Equal(1, await _fx.QueryAsync(db => db.Plants.CountAsync()));
        Assert.Equal(1, await _fx.QueryAsync(db => db.Producers.CountAsync()));
        var saved = await _fx.QueryAsync(db => db.Seeds.Include(s => s.SeedsInfo).FirstAsync(s => s.Id == seed.Id));
        Assert.Equal(50, saved.SeedsInfo.AmountSeeds);
    }

    // ── AddOrUpdateSeedAsync: новый пакет на СУЩЕСТВУЮЩЕМ растении (другой год) ──
    //    Критично: отсоединённое растение не должно дублироваться.

    [Fact]
    public async Task AddOrUpdateSeed_ExistingPlantDifferentYear_ReusesPlantNoDuplicate()
    {
        // Сидируем семя с растением Томат/Чёрный принц/Гавриш, годность 2026.
        await _fx.ExecuteAsync(async db =>
        {
            var seed = new Seed
            {
                Plant = MakePlant(),
                SeedsInfo = new SeedsInfo { AmountSeeds = 10, ExpirationDate = new DateTime(2026, 12, 31) }
            };
            seed.SeedsInfo.Seed = seed;
            db.Seeds.Add(seed);
            await db.SaveChangesAsync();
        });

        // VM-кэш: отсоединённый список существующих семян.
        var existing = await Seeds().GetAllSeedsAsync();

        // Тот же сорт/культура/производитель, но другой год → новый пакет на том же Plant.
        var request = new AddSeedRequest(
            "Томат", "Чёрный принц", "Гавриш", "Овощи",
            "Куплено", Units.PiecesOption,
            QuantityInPack: 30, PackCount: 1, CostPack: 0m,
            BestBy: new DateTime(2027, 12, 31), Note: null);

        var (_, isNew) = await Seeds().AddOrUpdateSeedAsync(request, existing);

        Assert.True(isNew);
        Assert.Equal(2, await _fx.QueryAsync(db => db.Seeds.CountAsync()));
        Assert.Equal(1, await _fx.QueryAsync(db => db.Plants.CountAsync()));     // НЕ продублировано
        Assert.Equal(1, await _fx.QueryAsync(db => db.PlantsCulture.CountAsync()));
        Assert.Equal(1, await _fx.QueryAsync(db => db.Producers.CountAsync()));
    }

    // ── Загрузить в одном UoW → изменить → сохранить в другом UoW ──────────────
    //    Критично: правка навигационного свойства отсоединённой сущности.

    [Fact]
    public async Task UpdateSeed_MutateNavigationOnDetachedEntity_PersistsAcrossUnitsOfWork()
    {
        await _fx.ExecuteAsync(async db =>
        {
            var seed = new Seed
            {
                Plant = MakePlant(),
                SeedsInfo = new SeedsInfo { AmountSeeds = 100, ExpirationDate = new DateTime(2027, 1, 1) }
            };
            seed.SeedsInfo.Seed = seed;
            db.Seeds.Add(seed);
            await db.SaveChangesAsync();
        });

        var loaded = await _fx.QueryAsync(db => db.Seeds.Include(s => s.SeedsInfo).FirstAsync());
        var id = loaded.Id;

        // UoW#1 — загрузка (сущность отсоединена после dispose).
        var seedFromService = await Seeds().GetSeedAsync(id);
        Assert.NotNull(seedFromService);

        // Правим навигационное свойство и сохраняем в UoW#2.
        seedFromService!.SeedsInfo.AmountSeeds = 42;
        await Seeds().UpdateSeed(seedFromService);

        var reloaded = await _fx.QueryAsync(db => db.Seeds.Include(s => s.SeedsInfo).FirstAsync(s => s.Id == id));
        Assert.Equal(42, reloaded.SeedsInfo.AmountSeeds);
    }

    // ── MakeASeedling со ссылкой на существующее отсоединённое растение ────────

    [Fact]
    public async Task MakeASeedling_ReferencesExistingDetachedPlant_PersistsWithoutDuplicate()
    {
        var plantId = await _fx.QueryAsync(async db =>
        {
            var plant = MakePlant();
            db.Plants.Add(plant);
            await db.SaveChangesAsync();
            return plant.Id;
        });

        var detachedPlant = await _fx.QueryAsync(db => db.Plants
            .Include(p => p.PlantCulture)
            .Include(p => p.PlantSort).ThenInclude(s => s.Producer)
            .FirstAsync(p => p.Id == plantId));

        var seedling = new Seedling
        {
            Plant = detachedPlant,
            Quantity = 5,
            SeedlingInfos = [new SeedlingInfo { LandingDate = DateTime.Today, SeedlingNumber = 0 }]
        };

        await Seedlings().MakeASeedling(seedling);

        Assert.Equal(1, await _fx.QueryAsync(db => db.Seedlings.CountAsync()));
        Assert.Equal(1, await _fx.QueryAsync(db => db.Plants.CountAsync())); // растение не продублировано
        var loaded = await _fx.QueryAsync(db => db.Seedlings.Include(s => s.SeedlingInfos).FirstAsync());
        Assert.Single(loaded.SeedlingInfos); // запись привязана к рассаде (FK проставлен)
    }

    // ── Germinate-сценарий: добавить запись всходов в отсоединённую рассаду ────

    [Fact]
    public async Task UpdateSeedling_AddNewInfoToDetachedSeedling_PersistsWithForeignKey()
    {
        await _fx.ExecuteAsync(async db =>
        {
            var seedling = new Seedling
            {
                Plant = MakePlant(),
                Quantity = 10,
                SeedlingInfos = [new SeedlingInfo { LandingDate = DateTime.Today, SeedlingNumber = 0 }]
            };
            db.Seedlings.Add(seedling);
            await db.SaveChangesAsync();
        });

        var detached = (await Seedlings().GetAllSeedlingsAsync()).Single();
        detached.SeedlingInfos.Add(new SeedlingInfo { GerminationDate = DateTime.Today, SeedlingNumber = 1 });

        await Seedlings().UpdateSeedling(detached);

        var reloaded = await _fx.QueryAsync(db => db.Seedlings.Include(s => s.SeedlingInfos).FirstAsync());
        Assert.Equal(2, reloaded.SeedlingInfos.Count);
        Assert.Equal(2, await _fx.QueryAsync(db => db.SeedlingInfos.CountAsync()));
    }

    // ── RebuildGrid на отсоединённом элементе ─────────────────────────────────

    [Fact]
    public async Task RebuildGrid_OnDetachedElement_CreatesCellsWithForeignKey()
    {
        var elementId = await _fx.QueryAsync(async db =>
        {
            var plan = new GardenPlan { Name = "П", Year = 2026 };
            db.GardenPlans.Add(plan);
            await db.SaveChangesAsync();
            var garden = new Garden { GardenPlanId = plan.Id, Name = "У", WidthMeters = 10, HeightMeters = 5, CanvasWidth = 400, CanvasHeight = 200 };
            db.Gardens.Add(garden);
            await db.SaveChangesAsync();
            var bed = new Bed { PlotId = garden.Id, Name = "Грядка", StateTypeName = "PlannedState" };
            bed.PlantingSpots.Add(new PlantingSpot { Row = 0, Column = 0 });
            db.GardenElements.Add(bed);
            await db.SaveChangesAsync();
            return bed.Id;
        });

        var detached = await Gardens().GetElementByIdAsync(elementId);
        Assert.NotNull(detached);

        await Gardens().RebuildGridAsync(detached!, rows: 2, cols: 2);

        var spotCount = await _fx.QueryAsync(db => db.PlantingSpots.CountAsync(s => s.GardenElementId == elementId));
        Assert.Equal(4, spotCount);
        var reloaded = await _fx.QueryAsync(db => db.GardenElements.FirstAsync(e => e.Id == elementId));
        Assert.Equal(2, reloaded.GridRows);
        Assert.Equal(2, reloaded.GridColumns);
    }
}
