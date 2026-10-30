using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    [Fact]
    public void GreenhouseEditor_IsolatedDraft_CancelReloadsCacheAndChildSelectionDoesNotDiscardInput()
    {
        var vm = CreateVm();
        var house = new GreenhouseFromViewModel { Id = 1, Name = "Исходная", WidthMeters = 6, HeightMeters = 3, X = 30 };
        vm.SelectedGreenhouse = house;
        var draft = vm.GreenhouseEditor!;
        draft.Name = "Черновик"; draft.WidthMeters = 5; draft.Note = "";
        vm.SelectedElement = new() { Id = 2 };
        vm.SelectedElement = null;
        Assert.Same(draft, vm.GreenhouseEditor);
        Assert.Equal(("Исходная", 6.0, 30.0), (house.Name, house.WidthMeters, house.X));
        Assert.Null(house.Note);
        house.WidthMeters = 7;

        vm.CancelGreenhousePropertiesCommand.Execute(null);

        Assert.NotSame(draft, vm.GreenhouseEditor);
        Assert.Equal(("Исходная", 7.0), (vm.GreenhouseEditor!.Name, vm.GreenhouseEditor.WidthMeters));
        Assert.Null(vm.GreenhouseEditor.Note);
        Assert.False(vm.GreenhouseEditor.IsDirty);
        vm.SelectedGreenhouse = null;
        Assert.Null(vm.GreenhouseEditor);
    }

    [Fact]
    public void SaveGreenhouseProperties_CleanOrInvalidBinding_DoesNotWrite()
    {
        var vm = CreateVm();
        vm.SelectedGreenhouse = new() { Id = 1, WidthMeters = 6, HeightMeters = 3 };
        Assert.False(vm.SaveGreenhouseSizeCommand.CanExecute(null));
        vm.GreenhouseEditor!.Name = "Новое";
        vm.GreenhouseEditorHasBindingErrors = true;
        vm.SaveGreenhouseSizeCommand.Execute(null);
        _gardens.DidNotReceive().EditGreenhousePropertiesAsync(Arg.Any<EditGreenhousePropertiesFromViewModel>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveGreenhouseProperties_SelectionChanges_UpdatesOnlyOriginalTarget(bool reselectOriginal)
    {
        var vm = CreateVm();
        var house = new GreenhouseFromViewModel { Id = 1, Name = "Первая", WidthMeters = 6, HeightMeters = 3 };
        var next = new GreenhouseFromViewModel { Id = 2, Name = "Вторая", WidthMeters = 8, HeightMeters = 4 };
        vm.SelectedGreenhouse = house;
        vm.GreenhouseEditor!.Name = "Сохранено";
        var submitted = vm.GreenhouseEditor.Values;
        var completion = new TaskCompletionSource<Result<GreenhousePropertiesResultFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(vm.IsGreenhouseEditorEnabled) && vm.IsGreenhouseEditorEnabled) finished.TrySetResult(); };
        _gardens.EditGreenhousePropertiesAsync(Arg.Any<EditGreenhousePropertiesFromViewModel>()).Returns(completion.Task);
        vm.SaveGreenhouseSizeCommand.Execute(null);
        Assert.False(vm.IsGreenhouseEditorEnabled);
        vm.SelectedGreenhouse = next;
        if (reselectOriginal) vm.SelectedGreenhouse = house;
        var active = vm.GreenhouseEditor;

        completion.SetResult(Result.Success(new GreenhousePropertiesResultFromViewModel(1, submitted, CanvasRectangle.Create(0, 0, 900, 450).ValueOrThrow())));
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("Сохранено", house.Name);
        Assert.Equal("Вторая", next.Name);
        Assert.Same(active, vm.GreenhouseEditor);
        Assert.Equal(reselectOriginal ? "Сохранено" : "Вторая", vm.GreenhouseEditor!.Name);
        Assert.False(vm.GreenhouseEditor.IsDirty);
    }

    [Fact]
    public async Task SaveGreenhouseProperties_LaterInput_IsRebasedAndExternalGeometryIsPreserved()
    {
        var vm = CreateVm();
        var house = new GreenhouseFromViewModel { Id = 1, WidthMeters = 6, HeightMeters = 3, X = 30, Y = 45, DisplayWidth = 200, DisplayHeight = 150 };
        vm.SelectedGreenhouse = house;
        var draft = vm.GreenhouseEditor!;
        draft.Name = "Отправлено";
        var submitted = draft.Values;
        var completion = new TaskCompletionSource<Result<GreenhousePropertiesResultFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(vm.IsGreenhouseEditorEnabled) && vm.IsGreenhouseEditorEnabled) finished.TrySetResult(); };
        _gardens.EditGreenhousePropertiesAsync(Arg.Any<EditGreenhousePropertiesFromViewModel>()).Returns(completion.Task);
        vm.SaveGreenhouseSizeCommand.Execute(null);
        draft.Note = "Поздний ввод";
        house.X = 90;

        completion.SetResult(Result.Success(new GreenhousePropertiesResultFromViewModel(1, submitted, CanvasRectangle.Create(0, 0, 900, 450).ValueOrThrow())));
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("Отправлено", draft.Original.Name);
        Assert.Equal("Поздний ввод", draft.Note);
        Assert.True(draft.IsDirty);
        Assert.Null(house.Note);
        Assert.Equal((90.0, 45.0, 200.0, 150.0), (house.X, house.Y, house.DisplayWidth, house.DisplayHeight));
    }
}
