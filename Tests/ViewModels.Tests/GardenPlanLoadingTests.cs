using BonfireDB.Entities.GardenPlanning;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    [Fact]
    public void LoadGardens_OlderResponse_DoesNotReplaceNewPlanOrDraft()
    {
        var vm = CreateVm();
        var pending = new TaskCompletionSource<IReadOnlyList<Garden>>();
        _gardens.GetGardensByPlanAsync(1).Returns(pending.Task);
        _gardens.GetGardensByPlanAsync(2).Returns(new[] { new Garden { Id = 20, Name = "Новый участок" } });
        var next = new GardenPlan { Id = 2 };
        vm.SelectedPlan = new GardenPlan { Id = 1 };
        vm.SelectedPlan = next;
        var garden = vm.SelectedGarden; var draft = vm.GardenEditor!; draft.Name = "Черновик";
        pending.SetResult(new[] { new Garden { Id = 10, Name = "Устаревший ответ" } });
        Assert.Same(next, vm.SelectedPlan); Assert.Same(garden, vm.SelectedGarden);
        Assert.Same(draft, vm.GardenEditor); Assert.Equal("Черновик", draft.Name);
        Assert.Equal(20, Assert.Single(vm.Gardens).Id);
    }

    [Fact]
    public void LoadGardens_ClearPlan_ClearsSelectionDraftAndGrid()
    {
        var vm = CreateVm(); vm.SelectedPlan = new GardenPlan { Id = 1 };
        var garden = GardenVm(); vm.Gardens = [garden]; vm.SelectedGarden = garden;
        var child = new GardenElementFromViewModel { Id = 2 }; garden.Elements.Add(child); vm.SelectedElement = child;
        vm.OpenPlantingGridCommand.Execute(null); vm.SelectedSpot = new() { Id = 3 };
        vm.SelectedPlan = null;
        Assert.Empty(vm.Gardens); Assert.Null(vm.SelectedGarden); Assert.Null(vm.GardenEditor);
        Assert.Null(vm.SelectedElement); Assert.Null(vm.ElementEditor); Assert.False(vm.IsGridOpen);
        Assert.Null(vm.EditingGridElement); Assert.Null(vm.SelectedSpot);
    }
}
