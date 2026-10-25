using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;
using BonfireDB.Entities.GardenPlanning.SpotStates;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    [Theory]
    [InlineData(2.5, 2, 8)]
    [InlineData(5, 2, 0)]
    public void PlantSpot_UsesFreshRemainingInsteadOfRequestedAmount(double requested, double consumed, double remaining)
    {
        var vm = CreateVm();
        var source = new PlantSourceItem { Kind = PlantSourceKind.Seedling, EntityId = 7, AvailableQty = 10, PlantName = "Томат" };
        vm.AvailableSeedlingItems = [source];
        vm.SelectedPlantSource = source;
        vm.PlantingAmount = requested;
        var date = vm.NewPlantingDate;
        _planting.PlantAsync(Arg.Any<PlantRequest>()).Returns(Result.Success(new PlantingResultFromViewModel(4, 9,
            SeedAmount.Create(consumed, SeedUnit.Pieces).ValueOrThrow(), SeedAmount.Create(remaining, SeedUnit.Pieces).ValueOrThrow())));
        var spot = new PlantingSpotFromViewModel { Id = 4 };

        vm.PlantSpotCommand.Execute(spot);

        Assert.Equal(remaining, source.AvailableQty);
        Assert.Equal(remaining > 0, vm.AvailableSeedlingItems.Contains(source));
        Assert.Null(vm.SelectedPlantSource);
        Assert.Equal(nameof(PlantedSpotState), spot.StateTypeName);
        Assert.Equal(date, spot.PlantedDate);
    }

    [Fact]
    public void PlantSpot_Refused_PreservesInputAndReportsReason()
    {
        var vm = CreateVm();
        var source = new PlantSourceItem { Kind = PlantSourceKind.Seed, EntityId = 7, AvailableQty = 10 };
        vm.AvailableSeedItems = [source];
        vm.SelectedPlantSource = source;
        vm.PlantingAmount = 3;
        _planting.PlantAsync(Arg.Any<PlantRequest>()).Returns(Result.Failure<PlantingResultFromViewModel>("Ячейка занята"));
        var spot = new PlantingSpotFromViewModel { Id = 4 };

        vm.PlantSpotCommand.Execute(spot);

        Assert.Same(source, vm.SelectedPlantSource);
        Assert.Equal(3, vm.PlantingAmount);
        Assert.Equal(10, source.AvailableQty);
        Assert.Equal(nameof(EmptySpotState), spot.StateTypeName);
        _dialog.Received(1).Warning("Ячейка занята", "Посадка");
    }
}
