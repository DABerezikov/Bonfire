namespace ViewModels.Tests;

public class GardenPlanViewModelTests
{
    private static GardenPlanViewModel CreateVm() => new(
        Substitute.For<IGardenService>(),
        Substitute.For<IUserDialog>(),
        Substitute.For<ISeedlingsService>(),
        Substitute.For<ISeedsService>(),
        Substitute.For<IPlantingService>());

    [Fact]
    public void PlantingAmount_ExceedsAvailable_ClampedToAvailable()
    {
        var vm = CreateVm();
        vm.SelectedPlantSource = new PlantSourceItem { AvailableQty = 3, IsWeightBased = false };

        vm.PlantingAmount = 10;

        Assert.Equal(3, vm.PlantingAmount);
    }

    [Fact]
    public void PlantingAmount_WithinAvailable_Unchanged()
    {
        var vm = CreateVm();
        vm.SelectedPlantSource = new PlantSourceItem { AvailableQty = 5, IsWeightBased = false };

        vm.PlantingAmount = 2;

        Assert.Equal(2, vm.PlantingAmount);
    }

    [Fact]
    public void PlantingAmount_NonPositive_DefaultsToOne()
    {
        var vm = CreateVm();
        vm.SelectedPlantSource = new PlantSourceItem { AvailableQty = 5, IsWeightBased = false };

        vm.PlantingAmount = 0;

        Assert.Equal(1, vm.PlantingAmount);
    }
}
