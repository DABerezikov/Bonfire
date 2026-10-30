using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Infrastructure.Commands.Base;
using BonfireDB.Entities.GardenPlanning;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    [Fact]
    public void DeleteElement_Success_ClosesGridForDeletedElement()
    {
        var vm = CreateVm();
        vm.SelectedGarden = GardenVm();
        var model = new GardenElementFromViewModel { Id = 1 };
        vm.SelectedGarden.Elements.Add(model);
        vm.SelectedElement = model;
        vm.OpenPlantingGridCommand.Execute(null);
        vm.SelectedSpot = new() { Id = 2 };
        _gardens.DeleteGardenElementAsync(Arg.Any<DeleteGardenElementFromViewModel>()).Returns(Result.Success(new ElementDeletionResultFromViewModel(1)));

        vm.DeleteElementCommand.Execute(null);

        Assert.False(vm.IsGridOpen);
        Assert.Null(vm.EditingGridElement);
        Assert.Null(vm.SelectedSpot);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DeleteElement_SelectionChanges_RemovesOnlyCapturedModel(bool inner, bool changeContainer)
    {
        var vm = CreateVm();
        var garden = GardenVm();
        var house = new GreenhouseFromViewModel { Id = 3 };
        garden.Greenhouses.Add(house);
        vm.SelectedGarden = garden;
        if (inner) vm.SelectedGreenhouse = house;
        var source = inner ? house.InnerElements : garden.Elements;
        var original = new GardenElementFromViewModel { Id = 1 };
        var next = new GardenElementFromViewModel { Id = 2 };
        source.Add(original);
        var pending = new TaskCompletionSource<Result<ElementDeletionResultFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _gardens.DeleteGardenElementAsync(Arg.Is<DeleteGardenElementFromViewModel>(r => r.ElementId == 1)).Returns(pending.Task);
        vm.SelectedElement = original;
        var command = (CommandAsync)vm.DeleteElementCommand;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        command.ExecutableChanged += (_, _) => { if (command.Executable) completed.TrySetResult(); };
        vm.DeleteElementCommand.Execute(null);
        Assert.Contains(original, source);
        Assert.False(vm.DeleteElementCommand.CanExecute(null));
        vm.DeleteElementCommand.Execute(null);
        await _gardens.Received(1).DeleteGardenElementAsync(Arg.Any<DeleteGardenElementFromViewModel>());
        if (changeContainer)
        {
            vm.SelectedGarden = new() { Id = 4 };
            vm.SelectedGreenhouse = inner ? new() { Id = 5 } : null;
        }
        var active = inner ? vm.SelectedGreenhouse!.InnerElements : vm.SelectedGarden!.Elements;
        active.Add(next);
        vm.SelectedElement = next;
        var nextDraft = vm.ElementEditor;
        vm.OpenPlantingGridCommand.Execute(null);
        var nextSpot = new PlantingSpotFromViewModel { Id = 9 };
        vm.SelectedSpot = nextSpot;

        pending.SetResult(Result.Success(new ElementDeletionResultFromViewModel(1)));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.DoesNotContain(original, source);
        Assert.Contains(next, active);
        Assert.Same(next, vm.SelectedElement);
        Assert.Same(nextDraft, vm.ElementEditor);
        Assert.True(vm.IsGridOpen);
        Assert.Same(next, vm.EditingGridElement);
        Assert.Same(nextSpot, vm.SelectedSpot);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeleteElement_Failure_PreservesCollectionDraftAndGrid(bool inner)
    {
        var vm = CreateVm();
        vm.SelectedGarden = GardenVm();
        if (inner) vm.SelectedGreenhouse = new() { Id = 3 };
        var elements = inner ? vm.SelectedGreenhouse!.InnerElements : vm.SelectedGarden.Elements;
        var model = new GardenElementFromViewModel { Id = 1, Name = "Исходное" };
        elements.Add(model);
        vm.SelectedElement = model;
        var draft = vm.ElementEditor!;
        draft.Name = "Черновик";
        vm.OpenPlantingGridCommand.Execute(null);
        var spot = new PlantingSpotFromViewModel { Id = 2 };
        vm.SelectedSpot = spot;
        _gardens.DeleteGardenElementAsync(Arg.Any<DeleteGardenElementFromViewModel>()).Returns(Result.Failure<ElementDeletionResultFromViewModel>("Отказ БД"));

        vm.DeleteElementCommand.Execute(null);

        Assert.Same(model, Assert.Single(elements));
        Assert.Same(model, vm.SelectedElement);
        Assert.Same(draft, vm.ElementEditor);
        Assert.Equal("Черновик", draft.Name);
        Assert.True(vm.IsGridOpen);
        Assert.Same(model, vm.EditingGridElement);
        Assert.Same(spot, vm.SelectedSpot);
        _dialog.Received(1).Warning("Отказ БД", "Удаление элемента");
    }

    [Fact]
    public void DeleteElement_NoSelection_DoesNotWrite()
    {
        var vm = CreateVm();
        Assert.False(vm.DeleteElementCommand.CanExecute(null));
        vm.DeleteElementCommand.Execute(null);
        _gardens.DidNotReceive().DeleteGardenElementAsync(Arg.Any<DeleteGardenElementFromViewModel>());
    }
}
