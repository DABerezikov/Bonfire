using System.ComponentModel;

namespace Models.Tests;

public class GardenPlanningViewModelTests
{
    // ── GardenElementFromViewModel — INPC ─────────────────────────────────────

    private static List<string> CollectChanges(GardenElementFromViewModel vm)
    {
        var raised = new List<string>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName ?? "");
        return raised;
    }

    private static List<string> CollectChanges(PlantingSpotFromViewModel vm)
    {
        var raised = new List<string>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName ?? "");
        return raised;
    }

    [Fact]
    public void GardenElement_StateTypeName_RaisesPropertyChangedForSelf()
    {
        var vm = new GardenElementFromViewModel();
        var raised = CollectChanges(vm);

        vm.StateTypeName = "PreparedState";

        Assert.Contains("StateTypeName", raised);
    }

    [Fact]
    public void GardenElement_StateTypeName_RaisesStateDisplayName()
    {
        var vm = new GardenElementFromViewModel();
        var raised = CollectChanges(vm);

        vm.StateTypeName = "ActiveState";

        Assert.Contains("StateDisplayName", raised);
    }

    [Fact]
    public void GardenElement_StateTypeName_RaisesStateColor()
    {
        var vm = new GardenElementFromViewModel();
        var raised = CollectChanges(vm);

        vm.StateTypeName = "FallowState";

        Assert.Contains("StateColor", raised);
    }

    [Fact]
    public void GardenElement_StateTypeName_RaisesCanAddPlanting()
    {
        var vm = new GardenElementFromViewModel();
        var raised = CollectChanges(vm);

        vm.StateTypeName = "ActiveState";

        Assert.Contains("CanAddPlanting", raised);
    }

    [Fact]
    public void GardenElement_StateTypeName_RaisesCanModifyGrid()
    {
        var vm = new GardenElementFromViewModel();
        var raised = CollectChanges(vm);

        vm.StateTypeName = "ActiveState";

        Assert.Contains("CanModifyGrid", raised);
    }

    [Fact]
    public void GardenElement_StateTypeName_RaisesAllCanGoToProperties()
    {
        var vm = new GardenElementFromViewModel();
        var raised = CollectChanges(vm);

        vm.StateTypeName = "PreparedState";

        Assert.Contains("CanGoToPlanned", raised);
        Assert.Contains("CanGoToPrepared", raised);
        Assert.Contains("CanGoToActive", raised);
        Assert.Contains("CanGoToFallow", raised);
        Assert.Contains("CanGoToResting", raised);
        Assert.Contains("CanGoToArchived", raised);
    }

    [Fact]
    public void GardenElement_StateTypeName_SameValue_DoesNotRaisePropertyChanged()
    {
        var vm = new GardenElementFromViewModel { };
        // Set to known value first
        vm.StateTypeName = "PreparedState";
        var raised = CollectChanges(vm);

        vm.StateTypeName = "PreparedState"; // same value, no change

        Assert.Empty(raised);
    }

    // ── CanGoTo* computed from StateTypeName ──────────────────────────────────

    [Fact]
    public void GardenElement_InPlannedState_CanGoToPrepared_IsTrue()
    {
        var vm = new GardenElementFromViewModel { };
        vm.StateTypeName = "PlannedState";

        Assert.True(vm.CanGoToPrepared);
    }

    [Fact]
    public void GardenElement_InPlannedState_CanGoToActive_IsFalse()
    {
        var vm = new GardenElementFromViewModel { };
        vm.StateTypeName = "PlannedState";

        Assert.False(vm.CanGoToActive);
    }

    [Fact]
    public void GardenElement_InActiveState_CanGoToFallow_IsTrue()
    {
        var vm = new GardenElementFromViewModel { };
        vm.StateTypeName = "ActiveState";

        Assert.True(vm.CanGoToFallow);
    }

    [Fact]
    public void GardenElement_InActiveState_CanGoToPlanned_IsFalse()
    {
        var vm = new GardenElementFromViewModel { };
        vm.StateTypeName = "ActiveState";

        Assert.False(vm.CanGoToPlanned);
    }

    [Fact]
    public void GardenElement_InArchivedState_CanGoToPlanned_IsTrue()
    {
        var vm = new GardenElementFromViewModel { };
        vm.StateTypeName = "ArchivedState";

        Assert.True(vm.CanGoToPlanned);
    }

    [Fact]
    public void GardenElement_InArchivedState_CanGoToActive_IsFalse()
    {
        var vm = new GardenElementFromViewModel { };
        vm.StateTypeName = "ArchivedState";

        Assert.False(vm.CanGoToActive);
    }

    // ── TypeLabel ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Bed", "Грядка")]
    [InlineData("ColdFrame", "Парник")]
    [InlineData("FlowerBed", "Цветник")]
    [InlineData("OpenGroundArea", "Открытый грунт")]
    [InlineData("Unknown", "Unknown")]
    public void GardenElement_TypeLabel_ReturnsCorrectLabel(string elementType, string expectedLabel)
    {
        var vm = new GardenElementFromViewModel { ElementType = elementType };

        Assert.Equal(expectedLabel, vm.TypeLabel);
    }

    // ── TypeColor ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Bed", "#388E3C")]
    [InlineData("ColdFrame", "#1976D2")]
    [InlineData("FlowerBed", "#E91E63")]
    [InlineData("OpenGroundArea", "#795548")]
    [InlineData("Unknown", "#607D8B")]
    public void GardenElement_TypeColor_ReturnsCorrectColor(string elementType, string expectedColor)
    {
        var vm = new GardenElementFromViewModel { ElementType = elementType };

        Assert.Equal(expectedColor, vm.TypeColor);
    }

    // ── TypeFill ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Bed", "#E8F5E9")]
    [InlineData("ColdFrame", "#E3F2FD")]
    [InlineData("FlowerBed", "#FCE4EC")]
    [InlineData("OpenGroundArea", "#EFEBE9")]
    [InlineData("Something", "#F5F5F5")]
    public void GardenElement_TypeFill_ReturnsCorrectFill(string elementType, string expectedFill)
    {
        var vm = new GardenElementFromViewModel { ElementType = elementType };

        Assert.Equal(expectedFill, vm.TypeFill);
    }

    // ── IsSelected INPC ───────────────────────────────────────────────────────

    [Fact]
    public void GardenElement_IsSelected_RaisesPropertyChanged()
    {
        var vm = new GardenElementFromViewModel();
        var raised = CollectChanges(vm);

        vm.IsSelected = true;

        Assert.Contains("IsSelected", raised);
    }

    [Fact]
    public void GardenElement_IsSelected_StoresValue()
    {
        var vm = new GardenElementFromViewModel { IsSelected = true };

        Assert.True(vm.IsSelected);
    }

    // ── PlantingSpotFromViewModel — INPC ──────────────────────────────────────

    [Fact]
    public void PlantingSpot_StateTypeName_RaisesPropertyChangedForSelf()
    {
        var vm = new PlantingSpotFromViewModel();
        var raised = CollectChanges(vm);

        vm.StateTypeName = "PlantedSpotState";

        Assert.Contains("StateTypeName", raised);
    }

    [Fact]
    public void PlantingSpot_StateTypeName_RaisesStateDisplayName()
    {
        var vm = new PlantingSpotFromViewModel();
        var raised = CollectChanges(vm);

        vm.StateTypeName = "PlantedSpotState";

        Assert.Contains("StateDisplayName", raised);
    }

    [Fact]
    public void PlantingSpot_StateTypeName_RaisesCellColor()
    {
        var vm = new PlantingSpotFromViewModel();
        var raised = CollectChanges(vm);

        vm.StateTypeName = "DeadSpotState";

        Assert.Contains("CellColor", raised);
    }

    [Fact]
    public void PlantingSpot_StateTypeName_RaisesCellBorderColor()
    {
        var vm = new PlantingSpotFromViewModel();
        var raised = CollectChanges(vm);

        vm.StateTypeName = "ReservedSpotState";

        Assert.Contains("CellBorderColor", raised);
    }

    [Fact]
    public void PlantingSpot_StateTypeName_RaisesCanPlant()
    {
        var vm = new PlantingSpotFromViewModel();
        var raised = CollectChanges(vm);

        vm.StateTypeName = "PlantedSpotState";

        Assert.Contains("CanPlant", raised);
    }

    [Fact]
    public void PlantingSpot_StateTypeName_RaisesAllCanGoToProperties()
    {
        var vm = new PlantingSpotFromViewModel();
        var raised = CollectChanges(vm);

        vm.StateTypeName = "PlantedSpotState";

        Assert.Contains("CanGoToReserved", raised);
        Assert.Contains("CanGoToPlanted", raised);
        Assert.Contains("CanGoToHarvested", raised);
        Assert.Contains("CanGoToDead", raised);
        Assert.Contains("CanGoToEmpty", raised);
    }

    [Fact]
    public void PlantingSpot_StateTypeName_SameValue_DoesNotRaisePropertyChanged()
    {
        var vm = new PlantingSpotFromViewModel();
        vm.StateTypeName = "PlantedSpotState";
        var raised = CollectChanges(vm);

        vm.StateTypeName = "PlantedSpotState";

        Assert.Empty(raised);
    }

    // ── CanGoTo* computed from StateTypeName ──────────────────────────────────

    [Fact]
    public void PlantingSpot_InEmptyState_CanGoToReserved_IsTrue()
    {
        var vm = new PlantingSpotFromViewModel { };
        vm.StateTypeName = "EmptySpotState";

        Assert.True(vm.CanGoToReserved);
    }

    [Fact]
    public void PlantingSpot_InEmptyState_CanGoToHarvested_IsFalse()
    {
        var vm = new PlantingSpotFromViewModel { };
        vm.StateTypeName = "EmptySpotState";

        Assert.False(vm.CanGoToHarvested);
    }

    [Fact]
    public void PlantingSpot_InPlantedState_CanGoToDead_IsTrue()
    {
        var vm = new PlantingSpotFromViewModel { };
        vm.StateTypeName = "PlantedSpotState";

        Assert.True(vm.CanGoToDead);
    }

    [Fact]
    public void PlantingSpot_InPlantedState_CanGoToEmpty_IsFalse()
    {
        var vm = new PlantingSpotFromViewModel { };
        vm.StateTypeName = "PlantedSpotState";

        Assert.False(vm.CanGoToEmpty);
    }

    [Fact]
    public void PlantingSpot_InDeadState_CanGoToEmpty_IsTrue()
    {
        var vm = new PlantingSpotFromViewModel { };
        vm.StateTypeName = "DeadSpotState";

        Assert.True(vm.CanGoToEmpty);
    }

    [Fact]
    public void PlantingSpot_InDeadState_CanGoToPlanted_IsFalse()
    {
        var vm = new PlantingSpotFromViewModel { };
        vm.StateTypeName = "DeadSpotState";

        Assert.False(vm.CanGoToPlanted);
    }

    // ── PlantingSpot IsSelected INPC ──────────────────────────────────────────

    [Fact]
    public void PlantingSpot_IsSelected_RaisesPropertyChanged()
    {
        var vm = new PlantingSpotFromViewModel();
        var raised = CollectChanges(vm);

        vm.IsSelected = true;

        Assert.Contains("IsSelected", raised);
    }

    // ── PlantingSpot PlantLabel INPC ──────────────────────────────────────────

    [Fact]
    public void PlantingSpot_PlantLabel_RaisesPropertyChanged()
    {
        var vm = new PlantingSpotFromViewModel();
        var raised = CollectChanges(vm);

        vm.PlantLabel = "Томат Черри";

        Assert.Contains("PlantLabel", raised);
    }

    [Fact]
    public void PlantingSpot_PlantLabel_StoresValue()
    {
        var vm = new PlantingSpotFromViewModel { PlantLabel = "Огурец" };

        Assert.Equal("Огурец", vm.PlantLabel);
    }

    // ── PlantingSpot computed properties ──────────────────────────────────────

    [Fact]
    public void PlantingSpot_InEmptyState_StateDisplayName_IsCorrect()
    {
        var vm = new PlantingSpotFromViewModel();
        vm.StateTypeName = "EmptySpotState";

        Assert.Equal("Свободно", vm.StateDisplayName);
    }

    [Fact]
    public void PlantingSpot_InDeadState_CellColor_IsCorrect()
    {
        var vm = new PlantingSpotFromViewModel();
        vm.StateTypeName = "DeadSpotState";

        Assert.Equal("#FFCDD2", vm.CellColor);
    }
}
