using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden.States;
using Bonfire.Domain.Garden.SpotStates;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StateChange_Refused_PreservesDisplayedStateAndReportsReason(bool greenhouse)
    {
        var vm = CreateVm();
        vm.SelectedGreenhouse = new() { Id = 8, StateTypeName = nameof(PlannedState) };
        vm.SelectedElement = new() { Id = 7, StateTypeName = nameof(PlannedState) };
        _gardens.ChangeStateAsync(Arg.Any<ChangeGardenStateFromViewModel>()).Returns(Result.Failure<GardenStateResultFromViewModel>("Состояние уже изменилось"));

        (greenhouse ? vm.ChangeGreenhouseStateCommand : vm.ChangeElementStateCommand).Execute(nameof(PreparedState));

        Assert.Equal(nameof(PlannedState), vm.SelectedElement.StateTypeName);
        Assert.Equal(nameof(PlannedState), vm.SelectedGreenhouse.StateTypeName);
        _dialog.Received(1).Warning("Состояние уже изменилось", "Состояние планировщика");
    }

    [Fact]
    public void ClearSpot_SavedResult_ClearsLinkAndDisplayButKeepsNote()
    {
        var vm = CreateVm();
        var spot = new PlantingSpotFromViewModel { Id = 7, StateTypeName = nameof(PlantedSpotState), SeedlingInfoId = 8,
            PlantedDate = DateTime.Today, PlantLabel = "Томат", Note = "История" };
        _gardens.ChangeStateAsync(Arg.Any<ChangeGardenStateFromViewModel>()).Returns(Result.Success<GardenStateResultFromViewModel>(
            new GardenStateResultFromViewModel.Spot(new(7, 0, 0, nameof(EmptySpotState), Option.None<int>(), Option.None<DateTime>(), Option.None<DateTime>(), Option.Some("История")))));

        vm.ClearSpotCommand.Execute(spot);

        Assert.Equal(nameof(EmptySpotState), spot.StateTypeName);
        Assert.Null(spot.SeedlingInfoId);
        Assert.Null(spot.PlantedDate);
        Assert.Null(spot.PlantLabel);
        Assert.Equal("История", spot.Note);
        _gardens.DidNotReceive().GetSpotAsync(Arg.Any<int>());
    }

    [Fact]
    public void SpotChange_Refused_PreservesCellAndReasonInput()
    {
        var vm = CreateVm();
        var spot = new PlantingSpotFromViewModel { Id = 7, StateTypeName = nameof(EmptySpotState), PlantLabel = "Прежнее" };
        vm.PlanPickerLabel = " Томат ";
        _gardens.ChangeStateAsync(Arg.Any<ChangeGardenStateFromViewModel>()).Returns(Result.Failure<GardenStateResultFromViewModel>("Ячейка занята"));

        vm.ReserveSpotCommand.Execute(spot);

        Assert.Equal(nameof(EmptySpotState), spot.StateTypeName);
        Assert.Equal("Прежнее", spot.PlantLabel);
        Assert.Equal(" Томат ", vm.PlanPickerLabel);
        _dialog.Received(1).Warning("Ячейка занята", "Состояние планировщика");
    }
}
