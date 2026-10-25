using System.Collections.Immutable;
using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using BonfireDB.Planting;
using BonfireDB.Entities.GardenPlanning;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    [Fact]
    public void LoadGardens_InvalidSnapshot_PreservesCurrentDraftAndReportsReason()
    {
        var vm = CreateVm(); var garden = GardenVm(); vm.Gardens = [garden]; vm.SelectedGarden = garden;
        var draft = vm.GardenEditor!; draft.Name = "Черновик";
        _gardens.ReadGardensAsync(1).Returns(Result.Failure<ImmutableArray<GardenReadFromViewModel>>("Элемент 7: неверная геометрия"));
        vm.SelectedPlan = new GardenPlanFromViewModel { Id = 1 };
        Assert.Same(garden, Assert.Single(vm.Gardens)); Assert.Same(draft, vm.GardenEditor); Assert.Equal("Черновик", draft.Name);
        _dialog.Received(1).Warning("Элемент 7: неверная геометрия", "Загрузка участков");
    }

    [Fact]
    public void LoadGardens_OlderResponse_DoesNotReplaceNewPlanOrDraft()
    {
        var vm = CreateVm();
        var pending = new TaskCompletionSource<Result<ImmutableArray<GardenReadFromViewModel>>>();
        _gardens.ReadGardensAsync(1).Returns(pending.Task);
        _gardens.ReadGardensAsync(2).Returns(Result.Success(ImmutableArray.Create(GardenReadDbMapper.ReadGarden(new Garden { Id = 20, Name = "Новый участок" }).ValueOrThrow())));
        var next = new GardenPlanFromViewModel { Id = 2 };
        vm.SelectedPlan = new GardenPlanFromViewModel { Id = 1 };
        vm.SelectedPlan = next;
        var garden = vm.SelectedGarden; var draft = vm.GardenEditor!; draft.Name = "Черновик";
        pending.SetResult(Result.Success(ImmutableArray.Create(GardenReadDbMapper.ReadGarden(new Garden { Id = 10, Name = "Устаревший ответ" }).ValueOrThrow())));
        Assert.Same(next, vm.SelectedPlan); Assert.Same(garden, vm.SelectedGarden);
        Assert.Same(draft, vm.GardenEditor); Assert.Equal("Черновик", draft.Name);
        Assert.Equal(20, Assert.Single(vm.Gardens).Id);
    }

    [Fact]
    public void LoadGardens_ClearPlan_ClearsSelectionDraftAndGrid()
    {
        var vm = CreateVm(); vm.SelectedPlan = new GardenPlanFromViewModel { Id = 1 };
        var garden = GardenVm(); vm.Gardens = [garden]; vm.SelectedGarden = garden;
        var child = new GardenElementFromViewModel { Id = 2 }; garden.Elements.Add(child); vm.SelectedElement = child;
        vm.OpenPlantingGridCommand.Execute(null); vm.SelectedSpot = new() { Id = 3 };
        vm.SelectedPlan = null;
        Assert.Empty(vm.Gardens); Assert.Null(vm.SelectedGarden); Assert.Null(vm.GardenEditor);
        Assert.Null(vm.SelectedElement); Assert.Null(vm.ElementEditor); Assert.False(vm.IsGridOpen);
        Assert.Null(vm.EditingGridElement); Assert.Null(vm.SelectedSpot);
    }
}
