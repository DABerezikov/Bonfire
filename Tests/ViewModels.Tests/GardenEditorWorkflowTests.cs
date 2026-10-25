using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    [Fact]
    public void GardenEditor_IsolatedDraft_CancelReloadsCacheAndChildSelectionDoesNotDiscardInput()
    {
        var vm = CreateVm();
        var garden = new GardenFromViewModel { Id = 1, Name = "Исходная", WidthMeters = 6, HeightMeters = 3, CanvasWidth = 900 };
        vm.SelectedGarden = garden;
        var draft = vm.GardenEditor!;
        draft.Name = "Черновик"; draft.WidthMeters = 5; draft.Note = "";
        vm.SelectedElement = new() { Id = 2 };
        vm.SelectedElement = null;
        Assert.Same(draft, vm.GardenEditor);
        Assert.Equal(("Исходная", 6.0, 900.0), (garden.Name, garden.WidthMeters, garden.CanvasWidth));
        Assert.Null(garden.Note);
        garden.WidthMeters = 7;

        vm.CancelGardenPropertiesCommand.Execute(null);

        Assert.NotSame(draft, vm.GardenEditor);
        Assert.Equal(("Исходная", 7.0), (vm.GardenEditor!.Name, vm.GardenEditor.WidthMeters));
        Assert.Null(vm.GardenEditor.Note);
        Assert.False(vm.GardenEditor.IsDirty);
        vm.SelectedGarden = null;
        Assert.Null(vm.GardenEditor);
    }

    [Fact]
    public void SaveGardenProperties_CleanOrInvalidBinding_DoesNotWrite()
    {
        var vm = CreateVm();
        vm.SelectedGarden = new() { Id = 1, WidthMeters = 6, HeightMeters = 3 };
        Assert.False(vm.SaveGardenSizeCommand.CanExecute(null));
        vm.GardenEditor!.Name = "Новое";
        vm.GardenEditorHasBindingErrors = true;
        vm.SaveGardenSizeCommand.Execute(null);
        _gardens.DidNotReceive().EditGardenPropertiesAsync(Arg.Any<EditGardenPropertiesFromViewModel>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveGardenProperties_SelectionChanges_UpdatesOnlyOriginalTarget(bool reselectOriginal)
    {
        var vm = CreateVm();
        var garden = new GardenFromViewModel { Id = 1, Name = "Первая", WidthMeters = 6, HeightMeters = 3 };
        var next = new GardenFromViewModel { Id = 2, Name = "Вторая", WidthMeters = 8, HeightMeters = 4 };
        vm.SelectedGarden = garden;
        vm.GardenEditor!.Name = "Сохранено";
        var submitted = vm.GardenEditor.Values;
        var completion = new TaskCompletionSource<Result<GardenPropertiesResultFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(vm.IsGardenEditorEnabled) && vm.IsGardenEditorEnabled) finished.TrySetResult(); };
        _gardens.EditGardenPropertiesAsync(Arg.Any<EditGardenPropertiesFromViewModel>()).Returns(completion.Task);
        vm.SaveGardenSizeCommand.Execute(null);
        Assert.False(vm.IsGardenEditorEnabled);
        vm.SelectedGarden = next;
        if (reselectOriginal) vm.SelectedGarden = garden;
        var active = vm.GardenEditor;

        completion.SetResult(Result.Success(new GardenPropertiesResultFromViewModel(1, submitted, CanvasRectangle.Create(0, 0, 900, 450).ValueOrThrow())));
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("Сохранено", garden.Name);
        Assert.Equal("Вторая", next.Name);
        Assert.Same(active, vm.GardenEditor);
        Assert.Equal(reselectOriginal ? "Сохранено" : "Вторая", vm.GardenEditor!.Name);
        Assert.False(vm.GardenEditor.IsDirty);
    }

    [Fact]
    public async Task SaveGardenProperties_LaterInput_IsRebasedAndChildBoundsAreUpdated()
    {
        var vm = CreateVm();
        var garden = new GardenFromViewModel { Id = 1, WidthMeters = 6, HeightMeters = 3,
            Elements = [new() { ContainerCanvasWidth = 900 }], Greenhouses = [new() { ContainerCanvasWidth = 900 }] };
        vm.SelectedGarden = garden;
        var draft = vm.GardenEditor!;
        draft.Name = "Отправлено";
        var submitted = draft.Values;
        var completion = new TaskCompletionSource<Result<GardenPropertiesResultFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(vm.IsGardenEditorEnabled) && vm.IsGardenEditorEnabled) finished.TrySetResult(); };
        _gardens.EditGardenPropertiesAsync(Arg.Any<EditGardenPropertiesFromViewModel>()).Returns(completion.Task);
        vm.SaveGardenSizeCommand.Execute(null);
        draft.Address = "Поздний ввод";
        completion.SetResult(Result.Success(new GardenPropertiesResultFromViewModel(1, submitted, CanvasRectangle.Create(0, 0, 1050, 600).ValueOrThrow())));
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("Отправлено", draft.Original.Name);
        Assert.Equal("Поздний ввод", draft.Address);
        Assert.True(draft.IsDirty);
        Assert.Null(garden.Address);
        Assert.Equal(1050, Assert.Single(garden.Elements).ContainerCanvasWidth);
        Assert.Equal(600, Assert.Single(garden.Greenhouses).ContainerCanvasHeight);
    }
}
