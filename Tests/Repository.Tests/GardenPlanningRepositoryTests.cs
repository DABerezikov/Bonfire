using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Entities.GardenPlanning.States;
using BonfireDB.Entities.GardenPlanning.SpotStates;

namespace Repository.Tests;

public class GardenPlanningRepositoryTests : IDisposable
{
    private readonly DbBonfire _db;

    public GardenPlanningRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DbBonfire>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new DbBonfire(options);
    }

    public void Dispose() => _db.Dispose();

    private DbRepository<GardenPlan> PlanRepo() => new(_db);

    // Helper: create a GardenPlan + Garden in DB, return the Garden's Id.
    private async Task<(int planId, int gardenId)> SeedGardenAsync(string planName = "Тест", int year = 2026)
    {
        var planRepo = PlanRepo();
        var plan = new GardenPlan { Name = planName, Year = year };
        await planRepo.AddAsync(plan);

        var garden = new Garden
        {
            GardenPlanId = plan.Id,
            Name = "Участок",
            WidthMeters = 10,
            HeightMeters = 5,
            CanvasWidth = 400,
            CanvasHeight = 200
        };
        _db.Gardens.Add(garden);
        await _db.SaveChangesAsync();

        return (plan.Id, garden.Id);
    }

    // ── GardenPlan CRUD ───────────────────────────────────────────────────────

    [Fact]
    public async Task AddGardenPlan_CanBeRetrieved()
    {
        var repo = PlanRepo();
        var plan = new GardenPlan { Name = "Огород 2026", Year = 2026 };

        await repo.AddAsync(plan);

        var retrieved = await repo.GetAsync(plan.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Огород 2026", retrieved!.Name);
        Assert.Equal(2026, retrieved.Year);
    }

    [Fact]
    public async Task AddTwoGardenPlans_GetByIdReturnsCorrectOne()
    {
        var repo = PlanRepo();
        var plan1 = new GardenPlan { Name = "План А", Year = 2025 };
        var plan2 = new GardenPlan { Name = "План Б", Year = 2026 };

        await repo.AddAsync(plan1);
        await repo.AddAsync(plan2);

        var retrieved = await repo.GetAsync(plan2.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("План Б", retrieved!.Name);
    }

    // ── GardenElement TPH discriminator ──────────────────────────────────────

    [Fact]
    public async Task AddBed_DiscriminatorIsBed()
    {
        var (_, gardenId) = await SeedGardenAsync();

        var bed = new Bed { Name = "Грядка 1", PlotId = gardenId };
        _db.Beds.Add(bed);
        await _db.SaveChangesAsync();

        // Reload from DB to verify discriminator
        var entry = await _db.GardenElements
            .FindAsync(bed.Id);

        Assert.NotNull(entry);
        Assert.IsType<Bed>(entry);
    }

    [Fact]
    public async Task AddBed_ItemTypeIsAccessibleAsBed()
    {
        var (_, gardenId) = await SeedGardenAsync();

        var bed = new Bed { Name = "Грядка 2", PlotId = gardenId, Orientation = "С-Ю" };
        _db.Beds.Add(bed);
        await _db.SaveChangesAsync();

        var reloaded = await _db.Beds.FindAsync(bed.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("С-Ю", reloaded!.Orientation);
    }

    [Fact]
    public async Task AddColdFrame_DiscriminatorIsColdFrame()
    {
        var (_, gardenId) = await SeedGardenAsync();

        var cf = new ColdFrame { Name = "Парник 1", PlotId = gardenId, CoverMaterial = "Плёнка" };
        _db.ColdFrames.Add(cf);
        await _db.SaveChangesAsync();

        var entry = await _db.GardenElements.FindAsync(cf.Id);
        Assert.NotNull(entry);
        Assert.IsType<ColdFrame>(entry);
    }

    [Fact]
    public async Task AddColdFrame_CoverMaterialPersists()
    {
        var (_, gardenId) = await SeedGardenAsync();

        var cf = new ColdFrame { Name = "Парник 2", PlotId = gardenId, CoverMaterial = "Спанбонд" };
        _db.ColdFrames.Add(cf);
        await _db.SaveChangesAsync();

        var reloaded = await _db.ColdFrames.FindAsync(cf.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("Спанбонд", reloaded!.CoverMaterial);
    }

    // ── PlantingSpot state persistence ────────────────────────────────────────

    [Fact]
    public async Task PlantingSpot_StateTypeName_PersistsThroughSaveAndReload()
    {
        var (_, gardenId) = await SeedGardenAsync();

        var bed = new Bed { Name = "Грядка", PlotId = gardenId };
        _db.Beds.Add(bed);
        await _db.SaveChangesAsync();

        var spot = new PlantingSpot
        {
            GardenElementId = bed.Id,
            Row = 0,
            Column = 0,
            StateTypeName = "PlantedSpotState"
        };
        _db.PlantingSpots.Add(spot);
        await _db.SaveChangesAsync();

        _db.ChangeTracker.Clear();

        var reloaded = await _db.PlantingSpots.FindAsync(spot.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("PlantedSpotState", reloaded!.StateTypeName);
    }

    // ── GardenElement.TransitionTo ─────────────────────────────────────────────

    [Fact]
    public async Task GardenElementTransitionTo_ValidTransition_UpdatesStateTypeName()
    {
        var (_, gardenId) = await SeedGardenAsync();

        var bed = new Bed { Name = "Грядка", PlotId = gardenId, StateTypeName = "PlannedState" };
        _db.Beds.Add(bed);
        await _db.SaveChangesAsync();

        bed.TransitionTo(new PreparedState());
        _db.Beds.Update(bed);
        await _db.SaveChangesAsync();

        _db.ChangeTracker.Clear();

        var reloaded = await _db.Beds.FindAsync(bed.Id);
        Assert.Equal("PreparedState", reloaded!.StateTypeName);
    }

    [Fact]
    public void GardenElementTransitionTo_InvalidTransition_ThrowsInvalidOperationException()
    {
        // Planned → Active is forbidden
        var bed = new Bed { StateTypeName = "PlannedState" };

        Assert.Throws<InvalidOperationException>(() => bed.TransitionTo(new ActiveState()));
    }

    // ── PlantingSpot.TransitionTo ─────────────────────────────────────────────

    [Fact]
    public async Task PlantingSpotTransitionTo_EmptyToReserved_SetsCorrectStateTypeName()
    {
        var (_, gardenId) = await SeedGardenAsync();

        var bed = new Bed { Name = "Грядка", PlotId = gardenId };
        _db.Beds.Add(bed);
        await _db.SaveChangesAsync();

        var spot = new PlantingSpot
        {
            GardenElementId = bed.Id,
            Row = 0,
            Column = 0,
            StateTypeName = "EmptySpotState"
        };
        _db.PlantingSpots.Add(spot);
        await _db.SaveChangesAsync();

        spot.TransitionTo(new ReservedSpotState());
        _db.PlantingSpots.Update(spot);
        await _db.SaveChangesAsync();

        _db.ChangeTracker.Clear();

        var reloaded = await _db.PlantingSpots.FindAsync(spot.Id);
        Assert.Equal("ReservedSpotState", reloaded!.StateTypeName);
    }
}
