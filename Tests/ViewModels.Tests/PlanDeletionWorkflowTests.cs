using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Infrastructure.Commands.Base;
using BonfireDB.Entities.GardenPlanning;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    [Fact]
    public async Task DeletePlan_SelectionChanges_RemovesCapturedPlanAndPreservesCurrentGarden()
    {
        var vm = CreateVm(); var plan = new GardenPlanFromViewModel { Id = 1 }; var next = new GardenPlanFromViewModel { Id = 2 };
        vm.Plans = [plan, next]; vm.SelectedPlan = plan;
        _dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var pending = new TaskCompletionSource<Result<GardenPlanDeletionResultFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _gardens.DeletePlanByIdAsync(Arg.Any<DeleteGardenPlanFromViewModel>()).Returns(pending.Task);
        var command = (CommandAsync)vm.DeletePlanCommand;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        command.ExecutableChanged += (_, _) => { if (command.Executable) completed.TrySetResult(); };
        vm.DeletePlanCommand.Execute(null);
        vm.SelectedPlan = next; var garden = GardenVm(); vm.Gardens = [garden]; vm.SelectedGarden = garden;
        var draft = vm.GardenEditor!; draft.Name = "Поздний ввод";
        pending.SetResult(Result.Success(new GardenPlanDeletionResultFromViewModel(1))); await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.DoesNotContain(plan, vm.Plans); Assert.Contains(next, vm.Plans);
        Assert.Same(next, vm.SelectedPlan); Assert.Same(garden, vm.SelectedGarden);
        Assert.Same(draft, vm.GardenEditor); Assert.Equal("Поздний ввод", draft.Name);
    }
    [Fact]
    public void DeletePlan_LastPlan_ClosesGridAndClearsGardenDraft()
    {
        var vm = CreateVm(); var plan = new GardenPlanFromViewModel { Id = 1 }; vm.Plans = [plan]; vm.SelectedPlan = plan;
        var garden = GardenVm(); vm.Gardens = [garden]; vm.SelectedGarden = garden;
        var element = new GardenElementFromViewModel { Id = 3 }; garden.Elements.Add(element); vm.SelectedElement = element;
        vm.OpenPlantingGridCommand.Execute(null); vm.SelectedSpot = new() { Id = 4 };
        _dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _gardens.DeletePlanByIdAsync(Arg.Any<DeleteGardenPlanFromViewModel>()).Returns(Result.Success(new GardenPlanDeletionResultFromViewModel(1)));
        vm.DeletePlanCommand.Execute(null);
        Assert.Empty(vm.Plans); Assert.Empty(vm.Gardens); Assert.Null(vm.SelectedPlan); Assert.Null(vm.SelectedGarden);
        Assert.Null(vm.GardenEditor); Assert.Null(vm.SelectedElement); Assert.Null(vm.ElementEditor);
        Assert.False(vm.IsGridOpen); Assert.Null(vm.EditingGridElement); Assert.Null(vm.SelectedSpot);
    }

    [Fact]
    public void DeletePlan_Failure_KeepsPlanGardenDraftAndGrid()
    {
        var vm = CreateVm(); var plan = new GardenPlanFromViewModel { Id = 1 }; vm.Plans = [plan]; vm.SelectedPlan = plan;
        var garden = GardenVm(); vm.Gardens = [garden]; vm.SelectedGarden = garden;
        var draft = vm.GardenEditor!; draft.Name = "Черновик";
        var element = new GardenElementFromViewModel { Id = 3 }; garden.Elements.Add(element); vm.SelectedElement = element;
        vm.OpenPlantingGridCommand.Execute(null); var childDraft = vm.ElementEditor;
        _dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _gardens.DeletePlanByIdAsync(Arg.Any<DeleteGardenPlanFromViewModel>()).Returns(Result.Failure<GardenPlanDeletionResultFromViewModel>("Нельзя удалить план с теплицами"));
        vm.DeletePlanCommand.Execute(null);
        Assert.Same(plan, Assert.Single(vm.Plans)); Assert.Same(plan, vm.SelectedPlan); Assert.Same(garden, vm.SelectedGarden);
        Assert.Same(draft, vm.GardenEditor); Assert.Equal("Черновик", draft.Name);
        Assert.Same(element, vm.SelectedElement); Assert.Same(childDraft, vm.ElementEditor); Assert.True(vm.IsGridOpen);
        _dialog.Received(1).Warning("Нельзя удалить план с теплицами", "Удаление плана");
    }
}

