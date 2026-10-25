using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden.SpotStates;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    [Fact]
    public void RebuildGrid_Refused_PreservesSelectionCellsAndInput()
    {
        var vm = CreateVm();
        var cell = new PlantingSpotFromViewModel { Id = 8, PlantLabel = "История" };
        vm.SelectedElement = new() { Id = 7, CanModifyGrid = true, GridRows = 2, GridColumns = 3, PlantingSpots = [cell] };
        vm.OpenPlantingGridCommand.Execute(null);
        vm.SelectSpotCommand.Execute(cell);
        _gardens.ResizeGridAsync(Arg.Any<ResizeGridFromViewModel>()).Returns(Result.Failure<GridResultFromViewModel>("Сетка заблокирована"));

        vm.RebuildGridCommand.Execute(null);

        Assert.Same(cell, vm.SelectedSpot);
        Assert.Same(cell, Assert.Single(vm.EditingGridElement!.PlantingSpots));
        Assert.Equal((2, 3), (vm.EditingGridElement.GridRows, vm.EditingGridElement.GridColumns));
        _dialog.Received(1).Warning("Сетка заблокирована", "Сетка посадок");
    }

    [Fact]
    public void RebuildGrid_SavedResult_ReplacesCellsWithoutLoadingEfGraph()
    {
        var vm = CreateVm();
        vm.SelectedElement = new() { Id = 7, CanModifyGrid = true, GridRows = 2, GridColumns = 1 };
        vm.OpenPlantingGridCommand.Execute(null);
        _gardens.ResizeGridAsync(Arg.Any<ResizeGridFromViewModel>()).Returns(Result.Success(new GridResultFromViewModel(7, 2, 1,
            [new(8, 0, 0, nameof(PlantedSpotState), Option.Some(9), Option.Some(new DateTime(2026, 5, 1)), Option.None<DateTime>(), Option.Some("История")),
             new(10, 1, 0, nameof(EmptySpotState), Option.None<int>(), Option.None<DateTime>(), Option.None<DateTime>(), Option.None<string>())])));

        vm.RebuildGridCommand.Execute(null);

        Assert.Equal(2, vm.EditingGridElement!.PlantingSpots.Count);
        var cell = vm.EditingGridElement.PlantingSpots[0];
        Assert.Equal(8, cell.Id);
        Assert.Equal(9, cell.SeedlingInfoId);
        Assert.Equal("История", cell.PlantLabel);
        Assert.Equal(nameof(PlantedSpotState), cell.StateTypeName);
        Assert.Null(vm.SelectedSpot);
    }
}
