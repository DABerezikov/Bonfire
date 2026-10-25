using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Infrastructure.Commands.Base;
using BonfireDB.Entities.GardenPlanning;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    private static ElementCreationResultFromViewModel CreatedElement(CreateGardenElementFromViewModel request)
        => new(new(12, new(request.Name, Option.None<string>(), Option.None<string>(), Option.None<string>(), Option.None<string>()),
            CanvasRectangle.Create(request.X, request.Y, request.Width, request.Height).ValueOrThrow(), 0), request.Kind.GetType().Name,
            CanvasRectangle.Create(0, 0, 3000, 2250).ValueOrThrow());

    private static GreenhouseCreationResultFromViewModel CreatedGreenhouse(CreateGreenhouseFromViewModel request)
    {
        var dimensions = PlotDimensions.Create(request.WidthMeters, request.HeightMeters, request.Scale).ValueOrThrow();
        return new(new(12, new(request.Name, Option.None<string>(), Option.None<string>(), request.WidthMeters, request.HeightMeters), dimensions.Canvas),
            CanvasRectangle.Create(request.X, request.Y, dimensions.Canvas.Width, dimensions.Canvas.Height).ValueOrThrow(),
            CanvasRectangle.Create(0, 0, 3000, 2250).ValueOrThrow());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateObject_Failure_KeepsCurrentSelectionAndDraft(bool greenhouse)
    {
        var vm = CreateVm(); vm.SelectedGarden = GardenVm();
        var element = new GardenElementFromViewModel { Id = 3 }; vm.SelectedGarden.Elements.Add(element); vm.SelectedElement = element;
        var draft = vm.ElementEditor!; draft.Name = "Черновик";
        _gardens.CreateElementFromRequestAsync(Arg.Any<CreateGardenElementFromViewModel>()).Returns(Result.Failure<ElementCreationResultFromViewModel>("Свежий отказ"));
        _gardens.CreateGreenhouseFromRequestAsync(Arg.Any<CreateGreenhouseFromViewModel>()).Returns(Result.Failure<GreenhouseCreationResultFromViewModel>("Свежий отказ"));
        (greenhouse ? vm.AddGreenhouseCommand : vm.AddElementCommand).Execute(null);
        Assert.Same(element, vm.SelectedElement); Assert.Same(draft, vm.ElementEditor); Assert.Equal("Черновик", draft.Name);
        Assert.Single(vm.SelectedGarden.Elements); Assert.Empty(vm.SelectedGarden.Greenhouses);
        _dialog.Received(1).Warning("Свежий отказ", greenhouse ? "Добавление теплицы" : "Добавление элемента");
    }

    [Fact]
    public async Task CreateElement_ContainerChanges_UpdatesOriginalCollectionAndKeepsNewEditor()
    {
        var vm = CreateVm(); var garden = GardenVm(); vm.SelectedGarden = garden;
        var pending = new TaskCompletionSource<Result<ElementCreationResultFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var legacy = new TaskCompletionSource<GardenElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _gardens.CreateElementFromRequestAsync(Arg.Any<CreateGardenElementFromViewModel>()).Returns(pending.Task);
        _gardens.AddElementAsync(Arg.Any<GardenElement>()).Returns(legacy.Task);
        var command = (CommandAsync)vm.AddElementCommand;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        command.ExecutableChanged += (_, _) => { if (command.Executable) completed.TrySetResult(); };
        vm.AddElementCommand.Execute(null);
        var next = GardenVm(); next.Id = 2; vm.SelectedGarden = next;
        var selected = new GardenElementFromViewModel { Id = 4 }; next.Elements.Add(selected); vm.SelectedElement = selected;
        var draft = vm.ElementEditor;
        pending.SetResult(Result.Success(CreatedElement(new(1, "Грядка 1", new GardenElementKind.Bed(), 0, 0, 300, 135))));
        legacy.SetResult(new Bed { Id = 12, Name = "Грядка 1" });
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(12, Assert.Single(garden.Elements).Id); Assert.Same(selected, Assert.Single(next.Elements));
        Assert.Same(selected, vm.SelectedElement); Assert.Same(draft, vm.ElementEditor);
        await _gardens.DidNotReceive().AddElementAsync(Arg.Any<GardenElement>());
    }

    [Fact]
    public async Task CreateGreenhouse_GardenChanges_UpdatesCapturedGardenOnly()
    {
        var vm = CreateVm(); var garden = GardenVm(); vm.SelectedGarden = garden;
        var pending = new TaskCompletionSource<Result<GreenhouseCreationResultFromViewModel>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var legacy = new TaskCompletionSource<Greenhouse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _gardens.CreateGreenhouseFromRequestAsync(Arg.Any<CreateGreenhouseFromViewModel>()).Returns(pending.Task);
        _gardens.AddGreenhouseAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>()).Returns(legacy.Task);
        var command = (CommandAsync)vm.AddGreenhouseCommand;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        command.ExecutableChanged += (_, _) => { if (command.Executable) completed.TrySetResult(); };
        vm.AddGreenhouseCommand.Execute(null);
        var next = GardenVm(); next.Id = 2; vm.SelectedGarden = next; var draft = vm.GardenEditor;
        pending.SetResult(Result.Success(CreatedGreenhouse(new(1, "Теплица 1", 6, 3, 150, 0, 0))));
        legacy.SetResult(new Greenhouse { Id = 12, Name = "Теплица 1" });
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(12, Assert.Single(garden.Greenhouses).Id); Assert.Empty(next.Greenhouses);
        Assert.Same(draft, vm.GardenEditor);
        await _gardens.DidNotReceive().AddGreenhouseAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>());
    }
}
