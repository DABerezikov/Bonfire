using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Infrastructure.Commands.Base;
using BonfireDB.Entities.GardenPlanning;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    [Fact]
    public void DeleteGreenhouse_Success_ClosesOwnOverlayAndGrid()
    {
        var vm = CreateVm(); vm.SelectedGarden = GardenVm();
        var house = new GreenhouseFromViewModel { Id = 2 };
        vm.SelectedGarden.Greenhouses.Add(house);
        vm.OpenGreenhouseCommand.Execute(house);
        vm.SelectedElement = new() { Id = 3 };
        house.InnerElements.Add(vm.SelectedElement);
        vm.OpenPlantingGridCommand.Execute(null);
        vm.SelectedSpot = new() { Id = 4 };
        _dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _gardens.DeleteGreenhouseByIdAsync(Arg.Any<DeleteGreenhouseFromViewModel>()).Returns(Result.Success(new GreenhouseDeletionResultFromViewModel(2)));

        vm.DeleteGreenhouseCommand.Execute(null);

        Assert.False(vm.IsEditingGreenhouse);
        Assert.False(vm.IsGridOpen);
        Assert.Null(vm.EditingGridElement);
        Assert.Null(vm.SelectedSpot);
        Assert.Null(vm.SelectedGreenhouse);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteGreenhouse_SelectionChanges_RemovesCapturedHouseAndKeepsNewEditors(bool changeGarden)
    {
        var vm = CreateVm();
        var garden = GardenVm(); vm.SelectedGarden = garden;
        var house = new GreenhouseFromViewModel { Id = 2 };
        garden.Greenhouses.Add(house);
        vm.OpenGreenhouseCommand.Execute(house);
        _dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var pending = new TaskCompletionSource<Result<GreenhouseDeletionResultFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _gardens.DeleteGreenhouseByIdAsync(Arg.Is<DeleteGreenhouseFromViewModel>(r => r.GreenhouseId == 2)).Returns(pending.Task);
        var command = (CommandAsync)vm.DeleteGreenhouseCommand;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        command.ExecutableChanged += (_, _) => { if (command.Executable) completed.TrySetResult(); };
        vm.DeleteGreenhouseCommand.Execute(null);
        Assert.Contains(house, garden.Greenhouses);
        Assert.False(vm.DeleteGreenhouseCommand.CanExecute(null));
        vm.DeleteGreenhouseCommand.Execute(null);
        await _gardens.Received(1).DeleteGreenhouseByIdAsync(Arg.Any<DeleteGreenhouseFromViewModel>());
        if (changeGarden) vm.SelectedGarden = new() { Id = 5 };
        var next = new GreenhouseFromViewModel { Id = 6 };
        vm.SelectedGarden!.Greenhouses.Add(next);
        vm.OpenGreenhouseCommand.Execute(next);
        vm.SelectedElement = new() { Id = 7 };
        next.InnerElements.Add(vm.SelectedElement);
        vm.OpenPlantingGridCommand.Execute(null);
        var draft = vm.GreenhouseEditor;
        var selected = vm.SelectedElement;

        pending.SetResult(Result.Success(new GreenhouseDeletionResultFromViewModel(2)));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.DoesNotContain(house, garden.Greenhouses);
        Assert.Contains(next, vm.SelectedGarden.Greenhouses);
        Assert.Same(next, vm.SelectedGreenhouse);
        Assert.Same(draft, vm.GreenhouseEditor);
        Assert.Same(selected, vm.SelectedElement);
        Assert.Same(selected, vm.EditingGridElement);
        Assert.True(vm.IsEditingGreenhouse); Assert.True(vm.IsGridOpen);
        await _gardens.DidNotReceive().GetGreenhouseByIdAsync(Arg.Any<int>());
        await _gardens.DidNotReceive().DeleteGreenhouseAsync(Arg.Any<Greenhouse>());
    }

    [Theory]
    [InlineData("Теплица уже удалена")]
    [InlineData("Нельзя удалить теплицу с вложенными теплицами")]
    public void DeleteGreenhouse_Failure_PreservesModelsDraftsAndEditors(string error)
    {
        var vm = CreateVm(); vm.SelectedGarden = GardenVm();
        var house = new GreenhouseFromViewModel { Id = 2, Name = "Теплица" };
        vm.SelectedGarden.Greenhouses.Add(house);
        vm.OpenGreenhouseCommand.Execute(house);
        var draft = vm.GreenhouseEditor!;
        draft.Name = "Черновик теплицы";
        var child = new GardenElementFromViewModel { Id = 3 };
        house.InnerElements.Add(child);
        vm.SelectedElement = child;
        var elementDraft = vm.ElementEditor!;
        elementDraft.Name = "Черновик элемента";
        vm.OpenPlantingGridCommand.Execute(null);
        var spot = new PlantingSpotFromViewModel { Id = 4 };
        vm.SelectedSpot = spot;
        _dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _gardens.DeleteGreenhouseByIdAsync(Arg.Any<DeleteGreenhouseFromViewModel>()).Returns(Result.Failure<GreenhouseDeletionResultFromViewModel>(error));

        vm.DeleteGreenhouseCommand.Execute(null);

        Assert.Same(house, Assert.Single(vm.SelectedGarden.Greenhouses));
        Assert.Same(child, Assert.Single(house.InnerElements));
        Assert.Same(house, vm.SelectedGreenhouse);
        Assert.Same(draft, vm.GreenhouseEditor);
        Assert.Equal("Черновик теплицы", draft.Name);
        Assert.Same(child, vm.SelectedElement);
        Assert.Same(elementDraft, vm.ElementEditor);
        Assert.Equal("Черновик элемента", elementDraft.Name);
        Assert.True(vm.IsEditingGreenhouse); Assert.True(vm.IsGridOpen);
        Assert.Same(child, vm.EditingGridElement); Assert.Same(spot, vm.SelectedSpot);
        _dialog.Received(1).YesNoQuestion("Удалить теплицу «Теплица»? Все элементы и посадки будут удалены.", "Удаление теплицы");
        _dialog.Received(1).Warning(error, "Удаление теплицы");
    }

    [Fact]
    public void DeleteGreenhouse_NoSelection_DoesNotAskOrWrite()
    {
        var vm = CreateVm();
        Assert.False(vm.DeleteGreenhouseCommand.CanExecute(null));
        vm.DeleteGreenhouseCommand.Execute(null);
        _dialog.DidNotReceive().YesNoQuestion(Arg.Any<string>(), Arg.Any<string>());
        _gardens.DidNotReceive().DeleteGreenhouseByIdAsync(Arg.Any<DeleteGreenhouseFromViewModel>());
    }
}
