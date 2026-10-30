using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Infrastructure.Commands.Base;
using BonfireDB.Entities.GardenPlanning;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    [Fact]
    public async Task CreateGarden_PlanChanges_AppendsToCapturedCollectionAndKeepsCurrentDraft()
    {
        var vm = CreateVm(); var plan = new GardenPlanFromViewModel { Id = 1 }; vm.SelectedPlan = plan;
        var gardens = vm.Gardens;
        var pending = new TaskCompletionSource<Result<GardenPropertiesResultFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _gardens.CreateGardenFromRequestAsync(Arg.Any<CreateGardenFromViewModel>()).Returns(pending.Task);
        var command = (CommandAsync)vm.CreateGardenCommand;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        command.ExecutableChanged += (_, _) => { if (command.Executable) completed.TrySetResult(); };
        vm.CreateGardenCommand.Execute(null);
        vm.SelectedPlan = new GardenPlanFromViewModel { Id = 2 }; var next = GardenVm(); vm.Gardens = [next]; vm.SelectedGarden = next;
        var draft = vm.GardenEditor;
        pending.SetResult(Result.Success(new GardenPropertiesResultFromViewModel(3,
            new("Новый участок", Option.None<string>(), Option.None<string>(), 20, 15), CanvasRectangle.Create(0, 0, 3000, 2250).ValueOrThrow())));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(3, Assert.Single(gardens).Id); Assert.Same(next, Assert.Single(vm.Gardens));
        Assert.Same(next, vm.SelectedGarden); Assert.Same(draft, vm.GardenEditor);
    }

    [Fact]
    public void CreateGarden_Failure_PreservesSelectionAndDraft()
    {
        var vm = CreateVm(); vm.SelectedPlan = new GardenPlanFromViewModel { Id = 1 };
        var garden = GardenVm(); vm.Gardens = [garden]; vm.SelectedGarden = garden; var draft = vm.GardenEditor;
        _gardens.CreateGardenFromRequestAsync(Arg.Any<CreateGardenFromViewModel>()).Returns(Result.Failure<GardenPropertiesResultFromViewModel>("План удалён"));
        vm.CreateGardenCommand.Execute(null);
        Assert.Same(garden, Assert.Single(vm.Gardens)); Assert.Same(draft, vm.GardenEditor);
        _dialog.Received(1).Warning("План удалён", "Создание участка");
    }

    [Fact]
    public void CreatePlan_Failure_PreservesExistingPlans()
    {
        var vm = CreateVm(); var plan = new GardenPlanFromViewModel { Id = 1 }; vm.Plans = [plan]; vm.SelectedPlan = plan;
        _gardens.CreateCurrentPlanAsync().Returns(Result.Failure<GardenPlanResultFromViewModel>("Отказ записи"));
        vm.CreatePlanCommand.Execute(null);
        Assert.Same(plan, Assert.Single(vm.Plans)); Assert.Same(plan, vm.SelectedPlan);
        _dialog.Received(1).Warning("Отказ записи", "Создание плана");
    }
}
