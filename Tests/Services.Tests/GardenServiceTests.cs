using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Entities.GardenPlanning.States;
using BonfireDB.Entities.GardenPlanning.SpotStates;

namespace Services.Tests;

public class GardenServiceTests
{
    private readonly IRepository<GardenPlan>    _plans       = Substitute.For<IRepository<GardenPlan>>();
    private readonly IRepository<Garden>        _gardens     = Substitute.For<IRepository<Garden>>();
    private readonly IRepository<Greenhouse>    _greenhouses = Substitute.For<IRepository<Greenhouse>>();
    private readonly IRepository<GardenElement> _elements    = Substitute.For<IRepository<GardenElement>>();
    private readonly IRepository<PlantingSpot>  _spots       = Substitute.For<IRepository<PlantingSpot>>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    public GardenServiceTests()
    {
        _uow.Repository<GardenPlan>().Returns(_plans);
        _uow.Repository<Garden>().Returns(_gardens);
        _uow.Repository<Greenhouse>().Returns(_greenhouses);
        _uow.Repository<GardenElement>().Returns(_elements);
        _uow.Repository<PlantingSpot>().Returns(_spots);
    }

    private GardenService CreateService() => new(_uow.ToFactory());

    // ── CreatePlanAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePlanAsync_CallsPlansAddAsync()
    {
        _plans.AddAsync(Arg.Any<GardenPlan>()).Returns(c => c.Arg<GardenPlan>());

        var svc = CreateService();
        await svc.CreatePlanAsync("Огород 2026", 2026);

        await _plans.Received(1).AddAsync(Arg.Any<GardenPlan>());
    }

    [Fact]
    public async Task CreatePlanAsync_SetsCorrectNameAndYear()
    {
        GardenPlan? captured = null;
        _plans.AddAsync(Arg.Do<GardenPlan>(p => captured = p)).Returns(c => c.Arg<GardenPlan>());

        var svc = CreateService();
        await svc.CreatePlanAsync("Основной", 2026);

        Assert.NotNull(captured);
        Assert.Equal("Основной", captured!.Name);
        Assert.Equal(2026, captured.Year);
    }

    // ── DeletePlanAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeletePlanAsync_CallsPlansRemoveAsyncWithPlanId()
    {
        var plan = new GardenPlan { Id = 42, Name = "Удалить", Year = 2025 };

        var svc = CreateService();
        await svc.DeletePlanAsync(plan);

        await _plans.Received(1).RemoveAsync(42);
    }

    // ── CreateGardenAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateGardenAsync_CallsGardensAddAsync()
    {
        _gardens.AddAsync(Arg.Any<Garden>()).Returns(c => c.Arg<Garden>());

        var svc = CreateService();
        await svc.CreateGardenAsync(1, "Участок А", 10, 5);

        await _gardens.Received(1).AddAsync(Arg.Any<Garden>());
    }

    [Fact]
    public async Task CreateGardenAsync_SetsCanvasFromMetersAndDefaultScale()
    {
        Garden? captured = null;
        _gardens.AddAsync(Arg.Do<Garden>(g => captured = g)).Returns(c => c.Arg<Garden>());

        var svc = CreateService();
        await svc.CreateGardenAsync(1, "Участок", 10, 5);

        Assert.NotNull(captured);
        Assert.Equal(10 * 150, captured!.CanvasWidth);
        Assert.Equal(5  * 150, captured.CanvasHeight);
    }

    [Fact]
    public async Task CreateGardenAsync_SetsCanvasFromMetersAndCustomScale()
    {
        Garden? captured = null;
        _gardens.AddAsync(Arg.Do<Garden>(g => captured = g)).Returns(c => c.Arg<Garden>());

        var svc = CreateService();
        await svc.CreateGardenAsync(1, "Участок", 10, 5, scale: 50);

        Assert.NotNull(captured);
        Assert.Equal(10 * 50, captured!.CanvasWidth);
        Assert.Equal(5  * 50, captured.CanvasHeight);
    }

    [Fact]
    public async Task CreateGardenAsync_SetsGardenPlanId()
    {
        Garden? captured = null;
        _gardens.AddAsync(Arg.Do<Garden>(g => captured = g)).Returns(c => c.Arg<Garden>());

        var svc = CreateService();
        await svc.CreateGardenAsync(7, "Участок", 10, 5);

        Assert.Equal(7, captured!.GardenPlanId);
    }

    // ── AddGreenhouseAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task AddGreenhouseAsync_CallsGreenhousesAddAsync()
    {
        _greenhouses.AddAsync(Arg.Any<Greenhouse>()).Returns(c => c.Arg<Greenhouse>());

        var svc = CreateService();
        await svc.AddGreenhouseAsync(1, "Теплица 1", 5, 3);

        await _greenhouses.Received(1).AddAsync(Arg.Any<Greenhouse>());
    }

    [Fact]
    public async Task AddGreenhouseAsync_SetsDisplayWidthAndHeight()
    {
        Greenhouse? captured = null;
        _greenhouses.AddAsync(Arg.Do<Greenhouse>(gh => captured = gh)).Returns(c => c.Arg<Greenhouse>());

        var svc = CreateService();
        await svc.AddGreenhouseAsync(1, "Теплица", 5, 3);

        Assert.NotNull(captured);
        Assert.Equal(5 * 150, captured!.DisplayWidth);
        Assert.Equal(3 * 150, captured.DisplayHeight);
    }

    [Fact]
    public async Task AddGreenhouseAsync_SetsCanvasWidthAndHeight()
    {
        Greenhouse? captured = null;
        _greenhouses.AddAsync(Arg.Do<Greenhouse>(gh => captured = gh)).Returns(c => c.Arg<Greenhouse>());

        var svc = CreateService();
        await svc.AddGreenhouseAsync(1, "Теплица", 5, 3);

        Assert.NotNull(captured);
        Assert.Equal(5 * 150, captured!.CanvasWidth);
        Assert.Equal(3 * 150, captured.CanvasHeight);
    }

    // ── ChangeElementStateAsync ───────────────────────────────────────────────

    [Fact]
    public async Task ChangeElementStateAsync_ValidTransition_UpdatesStateTypeName()
    {
        var bed = new Bed { StateTypeName = "PlannedState" };

        var svc = CreateService();
        await svc.ChangeElementStateAsync(bed, new PreparedState());

        Assert.Equal("PreparedState", bed.StateTypeName);
    }

    [Fact]
    public async Task ChangeElementStateAsync_ValidTransition_CallsElementsUpdateAsync()
    {
        var bed = new Bed { StateTypeName = "PlannedState" };

        var svc = CreateService();
        await svc.ChangeElementStateAsync(bed, new PreparedState());

        await _elements.Received(1).UpdateAsync(bed);
    }

    [Fact]
    public async Task ChangeElementStateAsync_InvalidTransition_ThrowsInvalidOperationException()
    {
        // Planned → Active is forbidden
        var bed = new Bed { StateTypeName = "PlannedState" };

        var svc = CreateService();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.ChangeElementStateAsync(bed, new ActiveState()));
    }

    [Fact]
    public async Task ChangeElementStateAsync_InvalidTransition_DoesNotCallUpdateAsync()
    {
        var bed = new Bed { StateTypeName = "PlannedState" };

        var svc = CreateService();
        try { await svc.ChangeElementStateAsync(bed, new ActiveState()); } catch { /* expected */ }

        await _elements.DidNotReceive().UpdateAsync(Arg.Any<GardenElement>());
    }

    // ── RebuildGridAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task RebuildGridAsync_ActiveState_ThrowsInvalidOperationException()
    {
        // ActiveState.CanModifyGrid == false
        var bed = new Bed { StateTypeName = "ActiveState" };

        var svc = CreateService();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RebuildGridAsync(bed, 3, 3));
    }

    [Fact]
    public async Task RebuildGridAsync_ActiveState_DoesNotCallElementsUpdateAsync()
    {
        var bed = new Bed { StateTypeName = "ActiveState" };

        var svc = CreateService();
        try { await svc.RebuildGridAsync(bed, 3, 3); } catch { /* expected */ }

        await _elements.DidNotReceive().UpdateAsync(Arg.Any<GardenElement>());
    }

    [Fact]
    public async Task RebuildGridAsync_PlannedState_SetsGridRowsAndColumns()
    {
        var bed = new Bed { StateTypeName = "PlannedState" };

        var svc = CreateService();
        await svc.RebuildGridAsync(bed, 4, 5);

        Assert.Equal(4, bed.GridRows);
        Assert.Equal(5, bed.GridColumns);
    }

    [Fact]
    public async Task RebuildGridAsync_PlannedState_CallsElementsUpdateAsync()
    {
        var bed = new Bed { StateTypeName = "PlannedState" };

        var svc = CreateService();
        await svc.RebuildGridAsync(bed, 2, 3);

        await _elements.Received(1).UpdateAsync(bed);
    }

    [Fact]
    public async Task RebuildGridAsync_RemovesOutOfBoundsSpots()
    {
        var spot0 = new PlantingSpot { Id = 1, Row = 0, Column = 0 };
        var spot1 = new PlantingSpot { Id = 2, Row = 3, Column = 0 }; // Row >= 3 → out of bounds
        var spot2 = new PlantingSpot { Id = 3, Row = 0, Column = 4 }; // Col >= 4 → out of bounds

        var bed = new Bed
        {
            StateTypeName = "PlannedState",
            PlantingSpots = [spot0, spot1, spot2]
        };

        var svc = CreateService();
        await svc.RebuildGridAsync(bed, rows: 3, cols: 4);

        await _spots.Received(1).RemoveAsync(2);
        await _spots.Received(1).RemoveAsync(3);
        await _spots.DidNotReceive().RemoveAsync(1);
    }

    [Fact]
    public async Task RebuildGridAsync_CreatesNewSpotsForMissingCells()
    {
        // Грядка без ячеек, просим 2×2 = 4 ячейки — все четыре должны создаться
        var bed = new Bed
        {
            Id = 7,
            StateTypeName = "PlannedState",
            PlantingSpots = []
        };

        var svc = CreateService();
        await svc.RebuildGridAsync(bed, rows: 2, cols: 2);

        // AddAsync должен быть вызван ровно 4 раза (0:0, 0:1, 1:0, 1:1)
        await _spots.Received(4).AddAsync(Arg.Any<PlantingSpot>());
    }

    [Fact]
    public async Task RebuildGridAsync_DoesNotDuplicateExistingSpots()
    {
        // Уже есть ячейка 0:0 — при создании 1×2 должны добавить только 0:1
        var existing = new PlantingSpot { Id = 1, Row = 0, Column = 0 };
        var bed = new Bed
        {
            Id = 8,
            StateTypeName = "PlannedState",
            PlantingSpots = [existing]
        };

        var svc = CreateService();
        await svc.RebuildGridAsync(bed, rows: 1, cols: 2);

        // Только один новый спот (0:1), существующий 0:0 не дублируется
        await _spots.Received(1).AddAsync(Arg.Is<PlantingSpot>(s => s.Row == 0 && s.Column == 1));
        await _spots.DidNotReceive().AddAsync(Arg.Is<PlantingSpot>(s => s.Row == 0 && s.Column == 0));
    }

    // ── ClearSpotAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ClearSpotAsync_ResetsStateTypeName()
    {
        var spot = new PlantingSpot { StateTypeName = "PlantedSpotState", SeedlingInfoId = 5 };

        var svc = CreateService();
        await svc.ClearSpotAsync(spot);

        Assert.Equal("EmptySpotState", spot.StateTypeName);
    }

    [Fact]
    public async Task ClearSpotAsync_ClearsSeedlingInfoId()
    {
        var spot = new PlantingSpot { StateTypeName = "PlantedSpotState", SeedlingInfoId = 5 };

        var svc = CreateService();
        await svc.ClearSpotAsync(spot);

        Assert.Null(spot.SeedlingInfoId);
    }

    [Fact]
    public async Task ClearSpotAsync_ClearsPlantedDate()
    {
        var spot = new PlantingSpot { StateTypeName = "PlantedSpotState", PlantedDate = DateTime.Today };

        var svc = CreateService();
        await svc.ClearSpotAsync(spot);

        Assert.Null(spot.PlantedDate);
    }

    [Fact]
    public async Task ClearSpotAsync_ClearsHarvestDate()
    {
        var spot = new PlantingSpot { StateTypeName = "HarvestedSpotState", HarvestDate = DateTime.Today };

        var svc = CreateService();
        await svc.ClearSpotAsync(spot);

        Assert.Null(spot.HarvestDate);
    }

    [Fact]
    public async Task ClearSpotAsync_CallsSpotsUpdateAsync()
    {
        var spot = new PlantingSpot { StateTypeName = "PlantedSpotState" };

        var svc = CreateService();
        await svc.ClearSpotAsync(spot);

        await _spots.Received(1).UpdateAsync(spot);
    }

    // ── ChangeSpotStateAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ChangeSpotStateAsync_TransitionsState()
    {
        var spot = new PlantingSpot { StateTypeName = "EmptySpotState" };

        var svc = CreateService();
        await svc.ChangeSpotStateAsync(spot, new PlantedSpotState());

        Assert.Equal("PlantedSpotState", spot.StateTypeName);
    }

    [Fact]
    public async Task ChangeSpotStateAsync_WithLabel_SetsNote()
    {
        var spot = new PlantingSpot { StateTypeName = "EmptySpotState" };

        var svc = CreateService();
        await svc.ChangeSpotStateAsync(spot, new PlantedSpotState(), plantLabel: "Томат");

        Assert.Equal("Томат", spot.Note);
    }

    [Fact]
    public async Task ChangeSpotStateAsync_WithoutLabel_DoesNotChangeNote()
    {
        var spot = new PlantingSpot { StateTypeName = "EmptySpotState", Note = "Оригинал" };

        var svc = CreateService();
        await svc.ChangeSpotStateAsync(spot, new PlantedSpotState(), plantLabel: null);

        Assert.Equal("Оригинал", spot.Note);
    }

    [Fact]
    public async Task ChangeSpotStateAsync_WithDate_SetsPlantedDate()
    {
        var spot = new PlantingSpot { StateTypeName = "EmptySpotState" };
        var date = new DateTime(2026, 5, 1);

        var svc = CreateService();
        await svc.ChangeSpotStateAsync(spot, new PlantedSpotState(), plantedDate: date);

        Assert.Equal(date, spot.PlantedDate);
    }

    [Fact]
    public async Task ChangeSpotStateAsync_WithoutDate_DoesNotChangePlantedDate()
    {
        var originalDate = new DateTime(2026, 4, 1);
        var spot = new PlantingSpot { StateTypeName = "EmptySpotState", PlantedDate = originalDate };

        var svc = CreateService();
        await svc.ChangeSpotStateAsync(spot, new PlantedSpotState(), plantedDate: null);

        Assert.Equal(originalDate, spot.PlantedDate);
    }

    [Fact]
    public async Task ChangeSpotStateAsync_CallsSpotsUpdateAsync()
    {
        var spot = new PlantingSpot { StateTypeName = "EmptySpotState" };

        var svc = CreateService();
        await svc.ChangeSpotStateAsync(spot, new PlantedSpotState());

        await _spots.Received(1).UpdateAsync(spot);
    }
}
