using BonfireDB.Entities.GardenPlanning.SpotStates;

namespace Models.Tests;

public class PlantingSpotStateTests
{
    // ── Factory ───────────────────────────────────────────────────────────────

    [Fact]
    public void From_EmptySpotState_ReturnsEmptySpotState()
    {
        var state = PlantingSpotState.From("EmptySpotState");
        Assert.IsType<EmptySpotState>(state);
    }

    [Fact]
    public void From_ReservedSpotState_ReturnsReservedSpotState()
    {
        var state = PlantingSpotState.From("ReservedSpotState");
        Assert.IsType<ReservedSpotState>(state);
    }

    [Fact]
    public void From_PlantedSpotState_ReturnsPlantedSpotState()
    {
        var state = PlantingSpotState.From("PlantedSpotState");
        Assert.IsType<PlantedSpotState>(state);
    }

    [Fact]
    public void From_HarvestedSpotState_ReturnsHarvestedSpotState()
    {
        var state = PlantingSpotState.From("HarvestedSpotState");
        Assert.IsType<HarvestedSpotState>(state);
    }

    [Fact]
    public void From_DeadSpotState_ReturnsDeadSpotState()
    {
        var state = PlantingSpotState.From("DeadSpotState");
        Assert.IsType<DeadSpotState>(state);
    }

    [Fact]
    public void From_UnknownName_ReturnsEmptySpotState()
    {
        var state = PlantingSpotState.From("NonExistentState");
        Assert.IsType<EmptySpotState>(state);
    }

    [Fact]
    public void From_EmptyString_ReturnsEmptySpotState()
    {
        var state = PlantingSpotState.From("");
        Assert.IsType<EmptySpotState>(state);
    }

    // ── Allowed transitions ───────────────────────────────────────────────────

    [Fact]
    public void Empty_CanTransitionTo_Reserved()
    {
        Assert.True(new EmptySpotState().CanTransitionTo(new ReservedSpotState()));
    }

    [Fact]
    public void Empty_CanTransitionTo_Planted()
    {
        Assert.True(new EmptySpotState().CanTransitionTo(new PlantedSpotState()));
    }

    [Fact]
    public void Reserved_CanTransitionTo_Planted()
    {
        Assert.True(new ReservedSpotState().CanTransitionTo(new PlantedSpotState()));
    }

    [Fact]
    public void Reserved_CanTransitionTo_Empty()
    {
        Assert.True(new ReservedSpotState().CanTransitionTo(new EmptySpotState()));
    }

    [Fact]
    public void Planted_CanTransitionTo_Harvested()
    {
        Assert.True(new PlantedSpotState().CanTransitionTo(new HarvestedSpotState()));
    }

    [Fact]
    public void Planted_CanTransitionTo_Dead()
    {
        Assert.True(new PlantedSpotState().CanTransitionTo(new DeadSpotState()));
    }

    [Fact]
    public void Harvested_CanTransitionTo_Empty()
    {
        Assert.True(new HarvestedSpotState().CanTransitionTo(new EmptySpotState()));
    }

    [Fact]
    public void Harvested_CanTransitionTo_Planted()
    {
        Assert.True(new HarvestedSpotState().CanTransitionTo(new PlantedSpotState()));
    }

    [Fact]
    public void Dead_CanTransitionTo_Empty()
    {
        Assert.True(new DeadSpotState().CanTransitionTo(new EmptySpotState()));
    }

    // ── Forbidden transitions ─────────────────────────────────────────────────

    [Fact]
    public void Empty_CannotTransitionTo_Harvested()
    {
        Assert.False(new EmptySpotState().CanTransitionTo(new HarvestedSpotState()));
    }

    [Fact]
    public void Empty_CannotTransitionTo_Dead()
    {
        Assert.False(new EmptySpotState().CanTransitionTo(new DeadSpotState()));
    }

    [Fact]
    public void Planted_CannotTransitionTo_Empty()
    {
        Assert.False(new PlantedSpotState().CanTransitionTo(new EmptySpotState()));
    }

    [Fact]
    public void Planted_CannotTransitionTo_Reserved()
    {
        Assert.False(new PlantedSpotState().CanTransitionTo(new ReservedSpotState()));
    }

    [Fact]
    public void Dead_CannotTransitionTo_Planted()
    {
        Assert.False(new DeadSpotState().CanTransitionTo(new PlantedSpotState()));
    }

    [Fact]
    public void Dead_CannotTransitionTo_Harvested()
    {
        Assert.False(new DeadSpotState().CanTransitionTo(new HarvestedSpotState()));
    }

    [Fact]
    public void Harvested_CannotTransitionTo_Dead()
    {
        Assert.False(new HarvestedSpotState().CanTransitionTo(new DeadSpotState()));
    }

    [Fact]
    public void Reserved_CannotTransitionTo_Harvested()
    {
        Assert.False(new ReservedSpotState().CanTransitionTo(new HarvestedSpotState()));
    }

    // ── State properties ──────────────────────────────────────────────────────

    [Fact]
    public void EmptySpotState_DisplayName_IsCorrect()
    {
        Assert.Equal("Свободно", new EmptySpotState().DisplayName);
    }

    [Fact]
    public void EmptySpotState_CellColor_IsTransparent()
    {
        Assert.Equal("Transparent", new EmptySpotState().CellColor);
    }

    [Fact]
    public void EmptySpotState_CanPlant_IsTrue()
    {
        Assert.True(new EmptySpotState().CanPlant);
    }

    [Fact]
    public void EmptySpotState_CanHarvest_IsFalse()
    {
        Assert.False(new EmptySpotState().CanHarvest);
    }

    [Fact]
    public void PlantedSpotState_CanPlant_IsFalse()
    {
        Assert.False(new PlantedSpotState().CanPlant);
    }

    [Fact]
    public void PlantedSpotState_CanHarvest_IsTrue()
    {
        Assert.True(new PlantedSpotState().CanHarvest);
    }

    [Fact]
    public void DeadSpotState_CellColor_IsCorrect()
    {
        Assert.Equal("#FFCDD2", new DeadSpotState().CellColor);
    }

    [Fact]
    public void DeadSpotState_CanPlant_IsTrue()
    {
        Assert.True(new DeadSpotState().CanPlant);
    }

    [Fact]
    public void DeadSpotState_CanHarvest_IsFalse()
    {
        Assert.False(new DeadSpotState().CanHarvest);
    }

    [Fact]
    public void ReservedSpotState_DisplayName_IsCorrect()
    {
        Assert.Equal("Запланировано", new ReservedSpotState().DisplayName);
    }

    [Fact]
    public void HarvestedSpotState_DisplayName_IsCorrect()
    {
        Assert.Equal("Убрано", new HarvestedSpotState().DisplayName);
    }

    [Fact]
    public void PlantedSpotState_DisplayName_IsCorrect()
    {
        Assert.Equal("Посажено", new PlantedSpotState().DisplayName);
    }
}
