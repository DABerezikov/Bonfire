using Bonfire.Infrastructure;
using BonfireDB.Entities.GardenPlanning;
using Bonfire.Domain.Garden.States;
using Bonfire.Domain.Garden.SpotStates;
using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeleteGarden_OnlyRemovesAfterConfirmation(bool confirmed)
    {
        var vm = CreateVm(); var garden = GardenVm(); vm.Gardens = [garden]; vm.SelectedGarden = garden;
        var entity = new Garden { Id = garden.Id };
        _gardens.GetGardenByIdAsync(garden.Id).Returns(entity);
        _dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(confirmed);
        vm.DeleteGardenCommand.Execute(null);
        Assert.Equal(confirmed ? 0 : 1, vm.Gardens.Count);
        _gardens.Received(confirmed ? 1 : 0).DeleteGardenAsync(entity);
    }

    [Fact]
    public void AddGreenhouse_UsesDefaultMetersAndLinksCanvasContainers()
    {
        var vm = CreateVm(); vm.SelectedGarden = GardenVm();
        _gardens.AddGreenhouseAsync(1, "Теплица 1", 6, 3, CanvasConstants.PixelsPerMeter, Arg.Any<double>(), Arg.Any<double>())
            .Returns(new Greenhouse { Id = 5, Name = "Теплица", WidthMeters = 6, HeightMeters = 3, CanvasWidth = 900, CanvasHeight = 450 });
        vm.AddGreenhouseCommand.Execute(null);
        var added = Assert.Single(vm.SelectedGarden.Greenhouses);
        Assert.Equal(5, added.Id);
        Assert.Same(vm.SelectedGarden.Elements, added.ContainerElements);
        Assert.Same(vm.SelectedGarden.Greenhouses, added.ContainerGreenhouses);
        Assert.Equal(vm.GardenZoom, added.CanvasZoom);
    }

    [Fact]
    public void AddGreenhouse_NoRoom_WarnsWithoutPersistence()
    {
        var vm = CreateVm(); vm.SelectedGarden = new() { CanvasWidth = 1, CanvasHeight = 1 };
        vm.AddGreenhouseCommand.Execute(null);
        Assert.Empty(vm.SelectedGarden.Greenhouses);
        _dialog.Received(1).Warning(Arg.Any<string>(), "Добавление теплицы");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeleteGreenhouse_RequiresConfirmation(bool confirmed)
    {
        var vm = CreateVm(); vm.SelectedGarden = GardenVm();
        var selected = new GreenhouseFromViewModel { Id = 2 };
        vm.SelectedGarden.Greenhouses.Add(selected); vm.SelectedGreenhouse = selected;
        var entity = new Greenhouse { Id = 2 };
        _gardens.GetGreenhouseByIdAsync(2).Returns(entity);
        _dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(confirmed);
        vm.DeleteGreenhouseCommand.Execute(null);
        Assert.Equal(confirmed ? 0 : 1, vm.SelectedGarden.Greenhouses.Count);
        _gardens.Received(confirmed ? 1 : 0).DeleteGreenhouseAsync(entity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SaveGreenhouseSize_UsesPortResultAndPreservesInputOnFailure(bool overflow)
    {
        var vm = CreateVm();
        var child = new GardenElementFromViewModel { X = 100000, Width = 10, Height = 10 };
        vm.SelectedGreenhouse = new() { Id = 2, WidthMeters = 6, HeightMeters = 3, InnerCanvasWidth = 900, InnerCanvasHeight = 450, InnerElements = [child] };
        var draft = vm.GreenhouseEditor!;
        draft.WidthMeters = 5; draft.Material = "Стекло";
        _gardens.EditGreenhousePropertiesAsync(Arg.Any<EditGreenhousePropertiesFromViewModel>()).Returns(overflow
            ? Result.Failure<GreenhousePropertiesResultFromViewModel>("Свежий элемент не помещается")
            : Result.Success(new GreenhousePropertiesResultFromViewModel(2, draft.Values, CanvasRectangle.Create(0, 0, 750, 450).ValueOrThrow())));

        vm.SaveGreenhouseSizeCommand.Execute(null);

        if (overflow)
        {
            _dialog.Received(1).Warning("Свежий элемент не помещается", "Изменение размера теплицы");
            Assert.Equal(6, vm.SelectedGreenhouse.WidthMeters);
            Assert.Same(draft, vm.GreenhouseEditor);
            Assert.True(draft.IsDirty);
        }
        else
        {
            Assert.Equal((5.0, 750.0), (vm.SelectedGreenhouse.WidthMeters, vm.SelectedGreenhouse.InnerCanvasWidth));
            Assert.Equal("Стекло", vm.SelectedGreenhouse.Material);
            Assert.Equal(750, child.ContainerCanvasWidth);
            Assert.Equal(450, child.ContainerCanvasHeight);
            Assert.False(draft.IsDirty);
        }
        _gardens.DidNotReceive().GetGreenhouseByIdAsync(Arg.Any<int>());
        _gardens.DidNotReceive().UpdateGreenhouseAsync(Arg.Any<Greenhouse>());
    }

    [Theory]
    [InlineData("Bed")]
    [InlineData("ColdFrame")]
    public void SaveElementProperties_PreservesGeometryAndSubtypeProperties(string type)
    {
        var vm = CreateVm();
        vm.SelectedElement = new() { Id = 1, ElementType = type, Width = 90, Height = 90 };
        var draft = vm.ElementEditor!;
        draft.X = 30; draft.Y = 45; draft.Width = 180;
        draft.Name = "Новое"; draft.Note = "Заметка"; draft.SoilType = "Суглинок";
        draft.Orientation = "Север"; draft.CoverMaterial = "Плёнка";
        _gardens.EditElementPropertiesAsync(Arg.Any<EditElementPropertiesFromViewModel>()).Returns(Result.Success(
            new ElementPropertiesResultFromViewModel(1, draft.Values.Details, CanvasRectangle.Create(30, 45, 180, 90).ValueOrThrow(), 0.7)));

        vm.SaveElementPropertiesCommand.Execute(null);

        Assert.Equal((30.0, 45.0, 180.0, 90.0), (vm.SelectedElement.X, vm.SelectedElement.Y, vm.SelectedElement.Width, vm.SelectedElement.Height));
        Assert.Equal(("Новое", "Суглинок", "Север", "Плёнка"), (vm.SelectedElement.Name, vm.SelectedElement.SoilType, vm.SelectedElement.Orientation, vm.SelectedElement.CoverMaterial));
        Assert.False(draft.IsDirty);
        _gardens.Received(1).EditElementPropertiesAsync(Arg.Is<EditElementPropertiesFromViewModel>(request => request.Id == 1 && request.Values.Width == 180 && request.Original.Width == 90));
        _gardens.DidNotReceive().GetElementByIdAsync(Arg.Any<int>());
        _gardens.DidNotReceive().UpdateElementAsync(Arg.Any<GardenElement>());
    }

    [Fact]
    public void ToggleLockGreenhouse_PersistsLockState()
    {
        var vm = CreateVm(); var model = new GreenhouseFromViewModel { Id = 1, X = 30, Y = 45, DisplayWidth = 300, DisplayHeight = 150, Name = "Новая", Note = "Заметка" };
        _gardens.SetLockAsync(Arg.Any<SetGardenLockFromViewModel>()).Returns(call =>
            Result.Success(new GardenLockResultFromViewModel(1, call.Arg<SetGardenLockFromViewModel>().IsLocked)));
        vm.ToggleLockGreenhouseCommand.Execute(model);
        Assert.True(model.IsLocked);
        vm.ToggleLockGreenhouseCommand.Execute(model);
        Assert.False(model.IsLocked);
        _gardens.Received(2).SetLockAsync(Arg.Any<SetGardenLockFromViewModel>());
        _gardens.DidNotReceive().GetGreenhouseByIdAsync(Arg.Any<int>());
        _gardens.DidNotReceive().UpdateGreenhouseAsync(Arg.Any<Greenhouse>());
    }

    [Fact]
    public void DeleteAndLockElement_UpdateSelectedCollectionAndPersistence()
    {
        var vm = CreateVm(); vm.SelectedGarden = GardenVm();
        var model = new GardenElementFromViewModel { Id = 1 };
        var entity = new Bed { Id = 1 };
        _gardens.GetElementByIdAsync(1).Returns(entity);
        vm.SelectedGarden.Elements.Add(model); vm.SelectedElement = model;
        _gardens.SetLockAsync(Arg.Any<SetGardenLockFromViewModel>()).Returns(Result.Success(new GardenLockResultFromViewModel(1, true)));
        vm.ToggleLockElementCommand.Execute(model);
        Assert.True(model.IsLocked); Assert.False(entity.IsLocked);
        _gardens.DeleteGardenElementAsync(Arg.Any<DeleteGardenElementFromViewModel>()).Returns(Result.Success(new ElementDeletionResultFromViewModel(1)));
        vm.DeleteElementCommand.Execute(null);
        Assert.Empty(vm.SelectedGarden.Elements); Assert.Null(vm.SelectedElement);
        _gardens.Received(1).DeleteGardenElementAsync(Arg.Is<DeleteGardenElementFromViewModel>(r => r.ElementId == 1));
        _gardens.DidNotReceive().DeleteElementAsync(Arg.Any<GardenElement>());
    }

    [Fact]
    public void GreenhouseState_ValidTransitionUpdatesDisplayedState()
    {
        var vm = CreateVm(); vm.SelectedGreenhouse = new() { Id = 1, StateTypeName = nameof(PlannedState) };
        _gardens.ChangeStateAsync(Arg.Any<ChangeGardenStateFromViewModel>()).Returns(Result.Success<GardenStateResultFromViewModel>(
            new GardenStateResultFromViewModel.Greenhouse(1, new PreparedState())));
        vm.ChangeGreenhouseStateCommand.Execute(nameof(PreparedState));
        Assert.Equal(nameof(PreparedState), vm.SelectedGreenhouse.StateTypeName);
        _gardens.Received(1).ChangeStateAsync(Arg.Is<ChangeGardenStateFromViewModel>(request => request.EntityId == 1));
    }

    [Fact]
    public void PlantingGrid_OpenSelectToggleRebuildAndClose()
    {
        var vm = CreateVm();
        vm.SelectedElement = new() { Id = 1, CanModifyGrid = true, GridRows = 2, GridColumns = 3 };
        _gardens.ResizeGridAsync(Arg.Any<ResizeGridFromViewModel>()).Returns(Result.Success(new GridResultFromViewModel(1, 2, 3,
            [new(5, 1, 2, nameof(EmptySpotState), Option.None<int>(), Option.None<DateTime>(), Option.None<DateTime>(), Option.None<string>())])));
        vm.OpenPlantingGridCommand.Execute(null);
        Assert.True(vm.IsGridOpen); Assert.Same(vm.SelectedElement, vm.EditingGridElement);
        var spot = new PlantingSpotFromViewModel { Id = 5 };
        vm.SelectSpotCommand.Execute(spot); Assert.Same(spot, vm.SelectedSpot);
        vm.SelectSpotCommand.Execute(spot); Assert.Null(vm.SelectedSpot);
        vm.RebuildGridCommand.Execute(null);
        Assert.Equal(5, Assert.Single(vm.EditingGridElement!.PlantingSpots).Id);
        _gardens.Received(1).ResizeGridAsync(Arg.Is<ResizeGridFromViewModel>(request => request.ElementId == 1 && request.Rows == 2 && request.Columns == 3));
        vm.CloseGridCommand.Execute(null); Assert.False(vm.IsGridOpen);
    }

    [Theory]
    [InlineData("Reserved")]
    [InlineData("Harvested")]
    [InlineData("Dead")]
    public void SpotTransitions_UpdateDisplayedStatusWithoutUsingInventory(string target)
    {
        var vm = CreateVm();
        _gardens.ChangeStateAsync(Arg.Any<ChangeGardenStateFromViewModel>()).Returns(Result.Success<GardenStateResultFromViewModel>(
            new GardenStateResultFromViewModel.Spot(new(1, 0, 0, target + "SpotState", Option.None<int>(), Option.None<DateTime>(), Option.None<DateTime>(), Option.Some("Томат Черри")))));
        vm.PlanPickerLabel = "Томат Черри";
        var spot = new PlantingSpotFromViewModel { Id = 1 };
        (target switch { "Reserved" => vm.ReserveSpotCommand, "Harvested" => vm.HarvestSpotCommand, _ => vm.MarkDeadSpotCommand }).Execute(spot);
        Assert.Equal(target + "SpotState", spot.StateTypeName);
        if (target == "Reserved") Assert.Equal("Томат Черри", spot.PlantLabel);
        _planting.DidNotReceive().PlantAsync(Arg.Any<PlantRequest>());
    }
}
