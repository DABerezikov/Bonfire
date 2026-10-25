using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    private static ElementPropertiesResultFromViewModel ElementReceipt(ElementEditorDraft draft)
        => new(draft.Id, draft.Values.Details, CanvasRectangle.Create(draft.X, draft.Y, draft.Width, draft.Height).ValueOrThrow(), 0.4);

    [Fact]
    public void ElementEditor_ChangesAndCancel_KeepCanvasIndependentAndReloadCurrentCache()
    {
        var vm = CreateVm();
        var model = new GardenElementFromViewModel { Id = 1, Name = "Исходное", X = 0, Note = null };
        vm.SelectedElement = model;
        var draft = vm.ElementEditor!;
        draft.Name = "Черновик"; draft.Note = ""; draft.X = 120;
        Assert.Equal(("Исходное", 0.0), (model.Name, model.X));
        Assert.Null(model.Note);
        Assert.True(draft.IsDirty);
        model.X = 30;

        vm.CancelElementPropertiesCommand.Execute(null);

        Assert.NotSame(draft, vm.ElementEditor);
        Assert.Equal(("Исходное", 30.0), (vm.ElementEditor!.Name, vm.ElementEditor.X));
        Assert.Null(vm.ElementEditor.Note);
        Assert.False(vm.ElementEditor.IsDirty);
        vm.SelectedElement = null;
        Assert.Null(vm.ElementEditor);
    }

    [Fact]
    public void SaveElementProperties_Refused_PreservesDraftAndCanvasAndShowsReason()
    {
        var vm = CreateVm();
        vm.SelectedElement = new() { Id = 1, Name = "Исходное" };
        var draft = vm.ElementEditor!;
        draft.Name = "Черновик"; draft.X = 120;
        _gardens.EditElementPropertiesAsync(Arg.Any<EditElementPropertiesFromViewModel>()).Returns(Result.Failure<ElementPropertiesResultFromViewModel>("Новая коллизия"));

        vm.SaveElementPropertiesCommand.Execute(null);

        Assert.Same(draft, vm.ElementEditor);
        Assert.True(draft.IsDirty);
        Assert.Equal(("Исходное", 0.0), (vm.SelectedElement.Name, vm.SelectedElement.X));
        Assert.Equal(("Черновик", 120.0), (draft.Name, draft.X));
        _dialog.Received(1).Warning("Новая коллизия", "Свойства элемента");
        Assert.True(vm.IsElementEditorEnabled);
    }

    [Fact]
    public void SaveElementProperties_BindingErrorOrCleanDraft_DoesNotWrite()
    {
        var vm = CreateVm();
        vm.SelectedElement = new() { Id = 1 };
        Assert.False(vm.SaveElementPropertiesCommand.CanExecute(null));
        vm.ElementEditor!.Name = "Черновик";
        vm.ElementEditorHasBindingErrors = true;
        vm.SaveElementPropertiesCommand.Execute(null);
        _gardens.DidNotReceive().EditElementPropertiesAsync(Arg.Any<EditElementPropertiesFromViewModel>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveElementProperties_SelectionChanges_PreservesCurrentEditor(bool reselectOriginal)
    {
        var vm = CreateVm();
        var model = new GardenElementFromViewModel { Id = 1, Name = "Первый" };
        var next = new GardenElementFromViewModel { Id = 2, Name = "Второй" };
        vm.SelectedElement = model;
        vm.ElementEditor!.Name = "Сохранённое";
        var receipt = ElementReceipt(vm.ElementEditor);
        var completion = new TaskCompletionSource<Result<ElementPropertiesResultFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(vm.IsElementEditorEnabled) && vm.IsElementEditorEnabled) finished.TrySetResult(); };
        _gardens.EditElementPropertiesAsync(Arg.Any<EditElementPropertiesFromViewModel>()).Returns(completion.Task);
        vm.SaveElementPropertiesCommand.Execute(null);
        Assert.False(vm.IsElementEditorEnabled);
        vm.SelectedElement = next;
        if (reselectOriginal) vm.SelectedElement = model;
        var active = vm.ElementEditor;

        completion.SetResult(Result.Success(receipt));
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("Сохранённое", model.Name);
        Assert.Same(active, vm.ElementEditor);
        Assert.Equal(reselectOriginal ? "Сохранённое" : "Второй", vm.ElementEditor!.Name);
        Assert.False(vm.ElementEditor.IsDirty);
    }

    [Fact]
    public async Task SaveElementProperties_LaterDraftChanges_AreRebasedWithoutLosingInput()
    {
        var vm = CreateVm();
        vm.SelectedElement = new() { Id = 1, Name = "Исходное" };
        var draft = vm.ElementEditor!;
        draft.Name = "Отправлено";
        var receipt = ElementReceipt(draft);
        var completion = new TaskCompletionSource<Result<ElementPropertiesResultFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(vm.IsElementEditorEnabled) && vm.IsElementEditorEnabled) finished.TrySetResult(); };
        _gardens.EditElementPropertiesAsync(Arg.Any<EditElementPropertiesFromViewModel>()).Returns(completion.Task);
        vm.SaveElementPropertiesCommand.Execute(null);
        draft.Note = "Более поздний ввод";

        completion.SetResult(Result.Success(receipt));
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("Отправлено", draft.Original.Details.Name);
        Assert.Equal("Более поздний ввод", draft.Note);
        Assert.Null(vm.SelectedElement.Note);
        Assert.True(draft.IsDirty);
    }

    [Fact]
    public void SaveGesture_RebasesUnchangedFieldsWithoutLosingDraftEdits()
    {
        var vm = CreateVm();
        vm.SelectedElement = new() { Id = 1, Name = "Исходное", Width = 90, Height = 90 };
        var draft = vm.ElementEditor!;
        draft.Name = "Черновик";
        draft.Width = 180;
        vm.SelectedElement.X = 30;
        _gardens.SaveGeometryAsync(Arg.Any<SaveGardenGeometryFromViewModel>()).Returns(Result.Success(new GeometryResultFromViewModel(1,
            CanvasRectangle.Create(30, 0, 90, 90).ValueOrThrow())));

        vm.SaveElementPositionCommand.Execute(vm.SelectedElement);

        Assert.Same(draft, vm.ElementEditor);
        Assert.Equal(("Черновик", 30.0, 180.0), (draft.Name, draft.X, draft.Width));
        Assert.Equal(("Исходное", 30.0, 90.0), (vm.SelectedElement.Name, vm.SelectedElement.X, vm.SelectedElement.Width));
        Assert.True(draft.IsDirty);
    }
}
