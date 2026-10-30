using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.GardenPlanning;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    [Fact]
    public void SaveElementPosition_Gesture_DoesNotSaveUnsubmittedProperties()
    {
        var vm = CreateVm();
        vm.SelectedElement = new() { Id = 1, X = 30, Y = 45, Width = 180, Height = 90,
            Name = "Черновик", Note = "Не сохранять", Orientation = "Юг" };
        _gardens.SaveGeometryAsync(Arg.Any<SaveGardenGeometryFromViewModel>()).Returns(Result.Success(new GeometryResultFromViewModel(1,
            CanvasRectangle.Create(30, 45, 180, 90).ValueOrThrow())));

        vm.SaveElementPositionCommand.Execute(null);

        Assert.Equal("Черновик", vm.SelectedElement!.Name);
        Assert.Equal("Не сохранять", vm.SelectedElement!.Note);
        Assert.Equal("Юг", vm.SelectedElement!.Orientation);
        _gardens.Received(1).SaveGeometryAsync(Arg.Is<SaveGardenGeometryFromViewModel>(request => request.EntityId == 1 && request.Target is GeometryTarget.Element && request.X == 30));
    }

    [Fact]
    public void SaveElementPosition_ExplicitGestureTarget_IgnoresSelectionAndPreservesDraftOnFailure()
    {
        var vm = CreateVm();
        vm.SelectedElement = new() { Id = 2 };
        var target = new GardenElementFromViewModel { Id = 1, X = 30, Width = 180, Name = "Черновик" };
        _gardens.SaveGeometryAsync(Arg.Any<SaveGardenGeometryFromViewModel>()).Returns(Result.Failure<GeometryResultFromViewModel>("Геометрия заблокирована"));

        vm.SaveElementPositionCommand.Execute(target);

        _gardens.Received(1).SaveGeometryAsync(Arg.Is<SaveGardenGeometryFromViewModel>(request => request.EntityId == 1));
        Assert.Equal(2, vm.SelectedElement.Id);
        Assert.Equal(30, target.X);
        Assert.Equal("Черновик", target.Name);
        _dialog.Received(1).Warning("Геометрия заблокирована", "Сохранение геометрии");
    }

    [Fact]
    public void SaveGreenhousePosition_UsesReceiptWithoutChangingInternalDimensionsOrProperties()
    {
        var vm = CreateVm();
        var target = new GreenhouseFromViewModel { Id = 1, Name = "Черновик", Note = "Оставить", WidthMeters = 6, HeightMeters = 3 };
        _gardens.SaveGeometryAsync(Arg.Any<SaveGardenGeometryFromViewModel>()).Returns(Result.Success(new GeometryResultFromViewModel(1,
            CanvasRectangle.Create(30, 45, 300, 150).ValueOrThrow())));

        vm.SaveGreenhousePositionCommand.Execute(target);

        Assert.Equal((30.0, 45.0, 300.0, 150.0), (target.X, target.Y, target.DisplayWidth, target.DisplayHeight));
        Assert.Equal((6.0, 3.0, "Черновик", "Оставить"), (target.WidthMeters, target.HeightMeters, target.Name, target.Note));
        _gardens.Received(1).SaveGeometryAsync(Arg.Is<SaveGardenGeometryFromViewModel>(request => request.Target is GeometryTarget.Greenhouse));
    }

    [Fact]
    public void SavePosition_LockedOrAbsentTarget_DoesNotWrite()
    {
        var vm = CreateVm();
        Assert.False(vm.SaveElementPositionCommand.CanExecute(null));
        Assert.False(vm.SaveGreenhousePositionCommand.CanExecute(null));
        Assert.False(vm.SaveElementPositionCommand.CanExecute(new GardenElementFromViewModel { IsLocked = true }));
        Assert.False(vm.SaveGreenhousePositionCommand.CanExecute(new GreenhouseFromViewModel { IsLocked = true }));
        _gardens.DidNotReceive().SaveGeometryAsync(Arg.Any<SaveGardenGeometryFromViewModel>());
    }

    [Fact]
    public async Task SaveElementPosition_SelectionChangesWhileSaving_AppliesReceiptToOriginalTarget()
    {
        var vm = CreateVm();
        var original = new GardenElementFromViewModel { Id = 1, X = 30 };
        var next = new GardenElementFromViewModel { Id = 2, X = 400 };
        var completion = new TaskCompletionSource<Result<GeometryResultFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var applied = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        original.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(original.X)) applied.TrySetResult(); };
        _gardens.SaveGeometryAsync(Arg.Any<SaveGardenGeometryFromViewModel>()).Returns(completion.Task);
        vm.SelectedElement = original;
        vm.SaveElementPositionCommand.Execute(null);
        vm.SelectedElement = next;

        completion.SetResult(Result.Success(new GeometryResultFromViewModel(1, CanvasRectangle.Create(45, 0, 90, 90).ValueOrThrow())));
        await applied.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(45, original.X);
        Assert.Equal(400, next.X);
        Assert.Same(next, vm.SelectedElement);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SavePosition_UnchangedReceipt_DoesNotRepeatPropertyNotifications(bool greenhouse)
    {
        var vm = CreateVm();
        var element = new GardenElementFromViewModel { Id = 1, X = 30, Y = 45, Width = 180, Height = 90 };
        var house = new GreenhouseFromViewModel { Id = 1, X = 30, Y = 45, DisplayWidth = 180, DisplayHeight = 90 };
        var notifications = 0;
        element.PropertyChanged += (_, _) => notifications++;
        house.PropertyChanged += (_, _) => notifications++;
        _gardens.SaveGeometryAsync(Arg.Any<SaveGardenGeometryFromViewModel>()).Returns(Result.Success(new GeometryResultFromViewModel(1,
            CanvasRectangle.Create(30, 45, 180, 90).ValueOrThrow())));

        if (greenhouse) vm.SaveGreenhousePositionCommand.Execute(house);
        else vm.SaveElementPositionCommand.Execute(element);

        Assert.Equal(0, notifications);
    }
}
