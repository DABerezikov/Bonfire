using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Infrastructure.Commands.Base;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Lock_MissingTarget_PreservesDisplayedState(bool greenhouse, bool locked)
    {
        var vm = CreateVm();
        var element = new GardenElementFromViewModel { Id = 1, IsLocked = locked };
        var house = new GreenhouseFromViewModel { Id = 1, IsLocked = locked };
        _gardens.SetLockAsync(Arg.Any<SetGardenLockFromViewModel>()).Returns(Result.Failure<GardenLockResultFromViewModel>("Объект уже удалён"));
        if (greenhouse) vm.ToggleLockGreenhouseCommand.Execute(house);
        else vm.ToggleLockElementCommand.Execute(element);
        Assert.Equal(locked, greenhouse ? house.IsLocked : element.IsLocked);
        _dialog.Received(1).Warning("Объект уже удалён", "Блокировка геометрии");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Lock_PendingWrite_PreservesUiThenUpdatesCapturedTarget(bool greenhouse, bool locked)
    {
        var vm = CreateVm();
        var element = new GardenElementFromViewModel { Id = 1, IsLocked = locked };
        var house = new GreenhouseFromViewModel { Id = 1, IsLocked = locked };
        vm.SelectedElement = element;
        vm.SelectedGreenhouse = house;
        var draft = vm.GreenhouseEditor!;
        draft.Name = "Черновик";
        var pending = new TaskCompletionSource<Result<GardenLockResultFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _gardens.SetLockAsync(Arg.Any<SetGardenLockFromViewModel>()).Returns(pending.Task);
        var command = greenhouse ? vm.ToggleLockGreenhouseCommand : vm.ToggleLockElementCommand;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var asyncCommand = (CommandAsync)command;
        asyncCommand.ExecutableChanged += (_, _) => { if (asyncCommand.Executable) completed.TrySetResult(); };

        object model = greenhouse ? house : element;
        command.Execute(model);
        Assert.Equal(locked, greenhouse ? house.IsLocked : element.IsLocked);
        Assert.False(command.CanExecute(model));
        command.Execute(model);
        await _gardens.Received(1).SetLockAsync(Arg.Is<SetGardenLockFromViewModel>(request => request.EntityId == 1 && request.IsLocked == !locked &&
            (greenhouse ? request.Target is GardenLockTarget.Greenhouse : request.Target is GardenLockTarget.Element)));
        var nextElement = new GardenElementFromViewModel { Id = 2 };
        var nextHouse = new GreenhouseFromViewModel { Id = 2 };
        vm.SelectedGreenhouse = nextHouse;
        vm.SelectedElement = nextElement;

        pending.SetResult(Result.Success(new GardenLockResultFromViewModel(1, !locked)));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(!locked, greenhouse ? house.IsLocked : element.IsLocked);
        Assert.False(nextElement.IsLocked); Assert.False(nextHouse.IsLocked);
        Assert.Equal("Черновик", draft.Name);
        Assert.Same(nextElement, vm.SelectedElement);
        Assert.Same(nextHouse, vm.SelectedGreenhouse);
        Assert.True(command.CanExecute(model));
        await _gardens.DidNotReceive().UpdateElementAsync(Arg.Any<BonfireDB.Entities.GardenPlanning.GardenElement>());
        await _gardens.DidNotReceive().UpdateGreenhouseAsync(Arg.Any<BonfireDB.Entities.GardenPlanning.Greenhouse>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Lock_ReceiptMatchesDisplayedState_DoesNotRaiseRedundantNotification(bool greenhouse)
    {
        var vm = CreateVm();
        var element = new GardenElementFromViewModel { Id = 1 };
        var house = new GreenhouseFromViewModel { Id = 1 };
        var changes = 0;
        element.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(element.IsLocked)) changes++; };
        house.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(house.IsLocked)) changes++; };
        _gardens.SetLockAsync(Arg.Any<SetGardenLockFromViewModel>()).Returns(Result.Success(new GardenLockResultFromViewModel(1, false)));
        if (greenhouse) vm.ToggleLockGreenhouseCommand.Execute(house);
        else vm.ToggleLockElementCommand.Execute(element);
        Assert.False(element.IsLocked); Assert.False(house.IsLocked);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void Lock_InvalidParameters_DoNotWrite()
    {
        var vm = CreateVm();
        vm.ToggleLockElementCommand.Execute(null);
        vm.ToggleLockGreenhouseCommand.Execute("не объект");
        _gardens.DidNotReceive().SetLockAsync(Arg.Any<SetGardenLockFromViewModel>());
    }
}
