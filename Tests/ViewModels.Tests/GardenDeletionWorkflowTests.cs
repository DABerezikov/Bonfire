using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Infrastructure.Commands.Base;
using BonfireDB.Entities.GardenPlanning;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteGarden_SelectionChanges_RemovesCapturedGardenAndKeepsNewDraft(bool changePlan)
    {
        var vm = CreateVm();
        var garden = GardenVm(); var next = new GardenFromViewModel { Id = 2 };
        vm.Gardens = [garden, next]; vm.SelectedGarden = garden;
        _dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var pending = new TaskCompletionSource<Result<GardenDeletionResultFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _gardens.DeleteGardenByIdAsync(Arg.Any<DeleteGardenFromViewModel>()).Returns(pending.Task);
        var command = (CommandAsync)vm.DeleteGardenCommand;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        command.ExecutableChanged += (_, _) => { if (command.Executable) completed.TrySetResult(); };
        vm.DeleteGardenCommand.Execute(null);
        var originalGardens = vm.Gardens;
        if (changePlan) { vm.SelectedPlan = new GardenPlanFromViewModel { Id = 3 }; vm.Gardens = [next]; }
        vm.SelectedGarden = next; var draft = vm.GardenEditor!; draft.Name = "Новый ввод";
        Assert.False(vm.DeleteGardenCommand.CanExecute(null));
        vm.DeleteGardenCommand.Execute(null);
        await _gardens.Received(1).DeleteGardenByIdAsync(Arg.Is<DeleteGardenFromViewModel>(r => r.GardenId == garden.Id));
        pending.SetResult(Result.Success(new GardenDeletionResultFromViewModel(garden.Id)));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.DoesNotContain(garden, originalGardens);
        Assert.Contains(next, vm.Gardens);
        Assert.Same(next, vm.SelectedGarden); Assert.Same(draft, vm.GardenEditor);
        Assert.Equal("Новый ввод", draft.Name);
    }
    [Fact]
    public void DeleteGarden_Success_ClosesOwnGridAndSelectsRemainingGarden()
    {
        var vm = CreateVm(); var garden = GardenVm(); var next = new GardenFromViewModel { Id = 2 };
        vm.Gardens = [garden, next]; vm.SelectedGarden = garden;
        var child = new GardenElementFromViewModel { Id = 3 }; garden.Elements.Add(child); vm.SelectedElement = child;
        vm.OpenPlantingGridCommand.Execute(null); vm.SelectedSpot = new() { Id = 4 };
        _dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _gardens.DeleteGardenByIdAsync(Arg.Any<DeleteGardenFromViewModel>()).Returns(Result.Success(new GardenDeletionResultFromViewModel(1)));
        vm.DeleteGardenCommand.Execute(null);
        Assert.Same(next, vm.SelectedGarden); Assert.Null(vm.SelectedElement);
        Assert.False(vm.IsGridOpen); Assert.Null(vm.EditingGridElement); Assert.Null(vm.SelectedSpot);
    }

    [Theory]
    [InlineData("Участок уже удалён")]
    [InlineData("Нельзя удалить участок с теплицами")]
    public void DeleteGarden_Failure_PreservesDraftSelectionAndGrid(string error)
    {
        var vm = CreateVm(); var garden = GardenVm(); vm.Gardens = [garden]; vm.SelectedGarden = garden;
        var draft = vm.GardenEditor!; draft.Name = "Черновик";
        var child = new GardenElementFromViewModel { Id = 3 }; garden.Elements.Add(child); vm.SelectedElement = child;
        var childDraft = vm.ElementEditor; vm.OpenPlantingGridCommand.Execute(null); vm.SelectedSpot = new() { Id = 4 };
        var spot = vm.SelectedSpot;
        _dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _gardens.DeleteGardenByIdAsync(Arg.Any<DeleteGardenFromViewModel>()).Returns(Result.Failure<GardenDeletionResultFromViewModel>(error));
        vm.DeleteGardenCommand.Execute(null);
        Assert.Same(garden, Assert.Single(vm.Gardens)); Assert.Same(garden, vm.SelectedGarden);
        Assert.Same(draft, vm.GardenEditor); Assert.Equal("Черновик", draft.Name);
        Assert.Same(child, vm.SelectedElement); Assert.Same(childDraft, vm.ElementEditor);
        Assert.True(vm.IsGridOpen); Assert.Same(child, vm.EditingGridElement); Assert.Same(spot, vm.SelectedSpot);
        _dialog.Received(1).Warning(error, "Удаление участка");
    }
}

