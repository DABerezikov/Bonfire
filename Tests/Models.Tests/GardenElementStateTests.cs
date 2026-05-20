using BonfireDB.Entities.GardenPlanning.States;

namespace Models.Tests;

public class GardenElementStateTests
{
    // ── Factory ───────────────────────────────────────────────────────────────

    [Fact]
    public void From_PlannedState_ReturnsPlannedState()
    {
        var state = GardenElementState.From("PlannedState");
        Assert.IsType<PlannedState>(state);
    }

    [Fact]
    public void From_PreparedState_ReturnsPreparedState()
    {
        var state = GardenElementState.From("PreparedState");
        Assert.IsType<PreparedState>(state);
    }

    [Fact]
    public void From_ActiveState_ReturnsActiveState()
    {
        var state = GardenElementState.From("ActiveState");
        Assert.IsType<ActiveState>(state);
    }

    [Fact]
    public void From_FallowState_ReturnsFallowState()
    {
        var state = GardenElementState.From("FallowState");
        Assert.IsType<FallowState>(state);
    }

    [Fact]
    public void From_RestingState_ReturnsRestingState()
    {
        var state = GardenElementState.From("RestingState");
        Assert.IsType<RestingState>(state);
    }

    [Fact]
    public void From_ArchivedState_ReturnsArchivedState()
    {
        var state = GardenElementState.From("ArchivedState");
        Assert.IsType<ArchivedState>(state);
    }

    [Fact]
    public void From_UnknownName_ReturnsPlannedState()
    {
        var state = GardenElementState.From("SomeGarbageValue");
        Assert.IsType<PlannedState>(state);
    }

    [Fact]
    public void From_EmptyString_ReturnsPlannedState()
    {
        var state = GardenElementState.From("");
        Assert.IsType<PlannedState>(state);
    }

    // ── Allowed transitions ───────────────────────────────────────────────────

    [Fact]
    public void Planned_CanTransitionTo_Prepared()
    {
        Assert.True(new PlannedState().CanTransitionTo(new PreparedState()));
    }

    [Fact]
    public void Planned_CanTransitionTo_Archived()
    {
        Assert.True(new PlannedState().CanTransitionTo(new ArchivedState()));
    }

    [Fact]
    public void Prepared_CanTransitionTo_Active()
    {
        Assert.True(new PreparedState().CanTransitionTo(new ActiveState()));
    }

    [Fact]
    public void Prepared_CanTransitionTo_Fallow()
    {
        Assert.True(new PreparedState().CanTransitionTo(new FallowState()));
    }

    [Fact]
    public void Prepared_CanTransitionTo_Archived()
    {
        Assert.True(new PreparedState().CanTransitionTo(new ArchivedState()));
    }

    [Fact]
    public void Active_CanTransitionTo_Fallow()
    {
        Assert.True(new ActiveState().CanTransitionTo(new FallowState()));
    }

    [Fact]
    public void Active_CanTransitionTo_Resting()
    {
        Assert.True(new ActiveState().CanTransitionTo(new RestingState()));
    }

    [Fact]
    public void Fallow_CanTransitionTo_Prepared()
    {
        Assert.True(new FallowState().CanTransitionTo(new PreparedState()));
    }

    [Fact]
    public void Fallow_CanTransitionTo_Resting()
    {
        Assert.True(new FallowState().CanTransitionTo(new RestingState()));
    }

    [Fact]
    public void Fallow_CanTransitionTo_Archived()
    {
        Assert.True(new FallowState().CanTransitionTo(new ArchivedState()));
    }

    [Fact]
    public void Resting_CanTransitionTo_Planned()
    {
        Assert.True(new RestingState().CanTransitionTo(new PlannedState()));
    }

    [Fact]
    public void Resting_CanTransitionTo_Prepared()
    {
        Assert.True(new RestingState().CanTransitionTo(new PreparedState()));
    }

    [Fact]
    public void Archived_CanTransitionTo_Planned()
    {
        Assert.True(new ArchivedState().CanTransitionTo(new PlannedState()));
    }

    // ── Forbidden transitions ─────────────────────────────────────────────────

    [Fact]
    public void Planned_CannotTransitionTo_Active()
    {
        Assert.False(new PlannedState().CanTransitionTo(new ActiveState()));
    }

    [Fact]
    public void Planned_CannotTransitionTo_Fallow()
    {
        Assert.False(new PlannedState().CanTransitionTo(new FallowState()));
    }

    [Fact]
    public void Planned_CannotTransitionTo_Resting()
    {
        Assert.False(new PlannedState().CanTransitionTo(new RestingState()));
    }

    [Fact]
    public void Active_CannotTransitionTo_Planned()
    {
        Assert.False(new ActiveState().CanTransitionTo(new PlannedState()));
    }

    [Fact]
    public void Active_CannotTransitionTo_Prepared()
    {
        Assert.False(new ActiveState().CanTransitionTo(new PreparedState()));
    }

    [Fact]
    public void Active_CannotTransitionTo_Archived()
    {
        Assert.False(new ActiveState().CanTransitionTo(new ArchivedState()));
    }

    [Fact]
    public void Archived_CannotTransitionTo_Active()
    {
        Assert.False(new ArchivedState().CanTransitionTo(new ActiveState()));
    }

    [Fact]
    public void Archived_CannotTransitionTo_Prepared()
    {
        Assert.False(new ArchivedState().CanTransitionTo(new PreparedState()));
    }

    [Fact]
    public void Archived_CannotTransitionTo_Fallow()
    {
        Assert.False(new ArchivedState().CanTransitionTo(new FallowState()));
    }

    [Fact]
    public void Resting_CannotTransitionTo_Active()
    {
        Assert.False(new RestingState().CanTransitionTo(new ActiveState()));
    }

    [Fact]
    public void Resting_CannotTransitionTo_Archived()
    {
        Assert.False(new RestingState().CanTransitionTo(new ArchivedState()));
    }

    // ── State properties ──────────────────────────────────────────────────────

    [Fact]
    public void PlannedState_DisplayName_IsCorrect()
    {
        Assert.Equal("Запланирована", new PlannedState().DisplayName);
    }

    [Fact]
    public void PlannedState_StatusColor_IsCorrect()
    {
        Assert.Equal("#9E9E9E", new PlannedState().StatusColor);
    }

    [Fact]
    public void PlannedState_CanModifyGrid_IsTrue()
    {
        Assert.True(new PlannedState().CanModifyGrid);
    }

    [Fact]
    public void PlannedState_CanAddPlanting_IsFalse()
    {
        Assert.False(new PlannedState().CanAddPlanting);
    }

    [Fact]
    public void ActiveState_CanModifyGrid_IsFalse()
    {
        Assert.False(new ActiveState().CanModifyGrid);
    }

    [Fact]
    public void ActiveState_CanAddPlanting_IsTrue()
    {
        Assert.True(new ActiveState().CanAddPlanting);
    }

    [Fact]
    public void ArchivedState_CanModifyGrid_IsFalse()
    {
        Assert.False(new ArchivedState().CanModifyGrid);
    }

    [Fact]
    public void ArchivedState_CanAddPlanting_IsFalse()
    {
        Assert.False(new ArchivedState().CanAddPlanting);
    }

    [Fact]
    public void PreparedState_CanModifyGrid_IsTrue()
    {
        Assert.True(new PreparedState().CanModifyGrid);
    }

    [Fact]
    public void PreparedState_CanAddPlanting_IsTrue()
    {
        Assert.True(new PreparedState().CanAddPlanting);
    }

    [Fact]
    public void FallowState_CanModifyGrid_IsTrue()
    {
        Assert.True(new FallowState().CanModifyGrid);
    }

    [Fact]
    public void FallowState_CanAddPlanting_IsFalse()
    {
        Assert.False(new FallowState().CanAddPlanting);
    }

    [Fact]
    public void RestingState_CanModifyGrid_IsFalse()
    {
        Assert.False(new RestingState().CanModifyGrid);
    }

    [Fact]
    public void RestingState_CanAddPlanting_IsFalse()
    {
        Assert.False(new RestingState().CanAddPlanting);
    }
}
