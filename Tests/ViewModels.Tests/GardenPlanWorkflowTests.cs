using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;
using Bonfire.Infrastructure;
using Bonfire.Models.Mappers;
using BonfireDB.Entities.GardenPlanning;
using Bonfire.Domain.Garden.States;
using Bonfire.Domain.Garden.SpotStates;

namespace ViewModels.Tests;

public partial class GardenPlanWorkflowTests
{
    private readonly IGardenService _gardens = Substitute.For<IGardenService>();
    private readonly IUserDialog _dialog = Substitute.For<IUserDialog>();
    private readonly ISeedsService _seeds = Substitute.For<ISeedsService>();
    private readonly ISeedlingsService _seedlings = Substitute.For<ISeedlingsService>();
    private readonly IPlantingService _planting = Substitute.For<IPlantingService>();

    private GardenPlanViewModel CreateVm()
    {
        _gardens.GetPlansOrderedByYearDescAsync().Returns(Array.Empty<GardenPlan>());
        _gardens.GetGardensByPlanAsync(Arg.Any<int>()).Returns(Array.Empty<Garden>());
        _seeds.GetAllSeedsAsync().Returns(Array.Empty<Seed>());
        _seedlings.GetAllSeedlingsAsync().Returns(Array.Empty<Seedling>());
        return new(_gardens, _dialog, _seedlings, _seeds, _planting);
    }

    private static GardenFromViewModel GardenVm() => new()
    {
        Id = 1, Name = "Участок", WidthMeters = 20, HeightMeters = 15,
        CanvasWidth = 20 * CanvasConstants.PixelsPerMeter, CanvasHeight = 15 * CanvasConstants.PixelsPerMeter
    };

    [Fact]
    public void LoadData_SelectsFirstPlanAndGardenAndBuildsAvailableInventory()
    {
        var vm = CreateVm();
        var plan = new GardenPlan { Id = 1, Name = "План" };
        _gardens.GetPlansOrderedByYearDescAsync().Returns(new[] { plan });
        _gardens.GetGardensByPlanAsync(1).Returns(new[] { new Garden { Id = 2, Name = "Участок", CanvasWidth = 3000, CanvasHeight = 2000 } });
        var plant = new Plant { PlantCulture = new PlantCulture { Name = "Томат" }, PlantSort = new PlantSort { Name = "Черри" } };
        _seeds.GetAllSeedsAsync().Returns(new[]
        {
            new Seed { Id = 3, Plant = plant, SeedsInfo = new SeedsInfo { AmountSeeds = 5 } },
            new Seed { Id = 4, Plant = plant, SeedsInfo = new SeedsInfo { AmountSeedsWeight = 0.5 } },
            new Seed { Id = 5, Plant = plant, SeedsInfo = new SeedsInfo() }
        });
        _seedlings.GetAllSeedlingsAsync().Returns(new[]
        {
            new Seedling { Id = 6, Plant = plant, SeedlingInfos = [new() { GerminationDate = DateTime.Today }, new() { GerminationDate = DateTime.Today, IsDead = true }] },
            new Seedling { Id = 7, Plant = plant, IsPlantedInBed = true, SeedlingInfos = [new() { GerminationDate = DateTime.Today }] }
        });

        vm.LoadDataCommand.Execute(null);

        Assert.Same(plan, vm.SelectedPlan);
        Assert.Equal(2, vm.SelectedGarden!.Id);
        Assert.Equal(2, vm.AvailableSeedItems.Count);
        Assert.Equal(0.5, vm.AvailableSeedItems.Single(s => s.EntityId == 4).AvailableQty);
        Assert.True(vm.AvailableSeedItems.Single(s => s.EntityId == 4).IsWeightBased);
        Assert.Equal(1, Assert.Single(vm.AvailableSeedlingItems).AvailableQty);
        Assert.Equal("Томат Черри", Assert.Single(vm.AvailablePlantLabels));
    }

    [Fact]
    public void CreatePlan_SelectsNewPlanAndLoadsItsGardens()
    {
        var vm = CreateVm();
        var plan = new GardenPlan { Id = 3, Name = "Новый" };
        _gardens.CreatePlanAsync(Arg.Any<string>(), DateTime.Now.Year).Returns(plan);
        vm.CreatePlanCommand.Execute(null);
        Assert.Same(plan, vm.SelectedPlan);
        Assert.Same(plan, Assert.Single(vm.Plans));
        _gardens.Received().GetGardensByPlanAsync(3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeletePlan_RequiresConfirmationAndSelectsRemainingPlan(bool confirm)
    {
        var vm = CreateVm();
        var first = new GardenPlan { Id = 1, Name = "Первый" };
        var second = new GardenPlan { Id = 2, Name = "Второй" };
        vm.Plans = [first, second]; vm.SelectedPlan = first;
        _dialog.YesNoQuestion(Arg.Any<string>(), Arg.Any<string>()).Returns(confirm);
        vm.DeletePlanCommand.Execute(null);
        Assert.Equal(confirm ? 1 : 2, vm.Plans.Count);
        Assert.Same(confirm ? second : first, vm.SelectedPlan);
        _gardens.Received(confirm ? 1 : 0).DeletePlanAsync(first);
    }

    [Fact]
    public void CreateGarden_UsesPlanAndDisplayScaleAndSelectsCreatedGarden()
    {
        var vm = CreateVm(); vm.SelectedPlan = new GardenPlan { Id = 3 };
        _gardens.CreateGardenAsync(3, "Новый участок", 20, 15, CanvasConstants.PixelsPerMeter)
            .Returns(new Garden { Id = 4, Name = "Новый участок", CanvasWidth = 3000, CanvasHeight = 2250 });
        vm.CreateGardenCommand.Execute(null);
        Assert.Equal(4, vm.SelectedGarden!.Id);
        Assert.Single(vm.Gardens);
    }

    [Theory]
    [InlineData("Bed", 2, 0.9)]
    [InlineData("ColdFrame", 1.2, 0.8)]
    [InlineData("FlowerBed", 0.6, 0.6)]
    [InlineData("OpenGroundArea", 2, 2)]
    public void AddElement_EachType_UsesCorrectSizeAndContainer(string type, double width, double height)
    {
        var vm = CreateVm(); vm.SelectedGarden = GardenVm(); vm.SelectedElementType = type;
        _gardens.AddElementAsync(Arg.Any<GardenElement>()).Returns(c => { var e = c.Arg<GardenElement>(); e.Id = 12; return e; });
        vm.AddElementCommand.Execute(null);
        var added = Assert.Single(vm.SelectedGarden.Elements);
        Assert.Equal(type, added.ElementType);
        Assert.Equal(width * CanvasConstants.PixelsPerMeter, added.Width);
        Assert.Equal(height * CanvasConstants.PixelsPerMeter, added.Height);
        Assert.Same(added, vm.SelectedElement);
        Assert.Same(vm.SelectedGarden.Elements, added.ContainerElements);
    }

    [Fact]
    public void AddElement_FullCanvas_WarnsAndDoesNotSave()
    {
        var vm = CreateVm(); vm.SelectedGarden = new GardenFromViewModel { Name = "Маленький", CanvasWidth = 1, CanvasHeight = 1 };
        vm.AddElementCommand.Execute(null);
        Assert.Empty(vm.SelectedGarden.Elements);
        _dialog.Received(1).Warning(Arg.Any<string>(), "Добавление элемента");
        _gardens.DidNotReceive().AddElementAsync(Arg.Any<GardenElement>());
    }

    [Fact]
    public void GreenhouseSelection_DoesNotOpenOverlayUntilExplicitOpen()
    {
        var vm = CreateVm(); vm.SelectedGarden = GardenVm();
        var first = new GreenhouseFromViewModel { Id = 1, Name = "Первая" };
        var second = new GreenhouseFromViewModel { Id = 2, Name = "Вторая" };
        vm.SelectGreenhouseCommand.Execute(first);
        Assert.True(first.IsSelected); Assert.False(vm.IsEditingGreenhouse);
        vm.OpenGreenhouseCommand.Execute(second);
        Assert.False(first.IsSelected); Assert.True(second.IsSelected); Assert.True(vm.IsEditingGreenhouse);
        vm.CloseGreenhouseCommand.Execute(null);
        Assert.False(second.IsSelected); Assert.False(vm.IsEditingGreenhouse); Assert.Null(vm.SelectedGreenhouse);
    }

    [Fact]
    public void ElementSelection_DeselectsPreviousAndUpdatesPropertiesPanel()
    {
        var vm = CreateVm(); vm.SelectedGarden = GardenVm();
        var first = new GardenElementFromViewModel { Id = 1 };
        var second = new GardenElementFromViewModel { Id = 2 };
        vm.SelectElementCommand.Execute(first);
        vm.SelectElementCommand.Execute(second);
        Assert.False(first.IsSelected); Assert.True(second.IsSelected); Assert.Null(vm.GardenPropertiesPanel);
        vm.DeselectElementCommand.Execute(null);
        Assert.False(second.IsSelected); Assert.Same(vm.SelectedGarden, vm.GardenPropertiesPanel);
    }

    [Theory]
    [InlineData(0.01, 0.1)]
    [InlineData(10, 5)]
    [InlineData(1.234, 1.23)]
    public void Zoom_ClampsAndPropagatesToElementsAndGreenhouses(double requested, double expected)
    {
        var vm = CreateVm(); vm.SelectedGarden = GardenVm();
        var child = new GardenElementFromViewModel();
        var greenhouse = new GreenhouseFromViewModel { InnerElements = [child] };
        vm.SelectedGarden.Greenhouses.Add(greenhouse);
        vm.GardenZoom = requested;
        Assert.Equal(expected, vm.GardenZoom);
        Assert.Equal(expected, greenhouse.CanvasZoom);
        Assert.Equal(expected, child.CanvasZoom);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SaveGardenSize_RejectsOutOfBoundsElementsOrGreenhouses(bool greenhouse)
    {
        var vm = CreateVm(); vm.SelectedGarden = GardenVm();
        vm.SelectedGarden.WidthMeters = 1;
        if (greenhouse) vm.SelectedGarden.Greenhouses.Add(new() { X = 10000, DisplayWidth = 200 });
        else vm.SelectedGarden.Elements.Add(new() { X = 10000, Width = 200 });
        vm.SaveGardenSizeCommand.Execute(null);
        _dialog.Received(1).Warning(Arg.Any<string>(), "Изменение размера участка");
        _gardens.DidNotReceive().UpdateGardenAsync(Arg.Any<Garden>());
    }

    [Fact]
    public void SaveGardenSize_PersistsDimensionsAndUpdatesChildrenLimits()
    {
        var vm = CreateVm(); vm.SelectedGarden = GardenVm();
        vm.SelectedGarden.Elements.Add(new() { Width = 10, Height = 10 });
        vm.SelectedGarden.Greenhouses.Add(new() { DisplayWidth = 10, DisplayHeight = 10 });
        var entity = new Garden { Id = 1 };
        _gardens.GetGardenByIdAsync(1).Returns(entity);
        vm.SaveGardenSizeCommand.Execute(null);
        Assert.Equal(20 * CanvasConstants.PixelsPerMeter, entity.CanvasWidth);
        Assert.Equal(entity.CanvasWidth, vm.SelectedGarden.Elements[0].ContainerCanvasWidth);
        Assert.Equal(entity.CanvasHeight, vm.SelectedGarden.Greenhouses[0].ContainerCanvasHeight);
        _gardens.Received(1).UpdateGardenAsync(entity);
    }

    [Fact]
    public void ElementState_ValidTransitionSavesAndInvalidTransitionDoesNothing()
    {
        var vm = CreateVm(); vm.SelectedElement = new() { Id = 1, StateTypeName = nameof(PlannedState) };
        var entity = new Bed { Id = 1 };
        _gardens.GetElementByIdAsync(1).Returns(entity);
        vm.ChangeElementStateCommand.Execute(nameof(PreparedState));
        Assert.Equal(nameof(PreparedState), vm.SelectedElement.StateTypeName);
        _gardens.Received(1).ChangeElementStateAsync(entity, Arg.Is<GardenElementState>(s => s is PreparedState));
        vm.ChangeElementStateCommand.Execute(nameof(PlannedState));
        _gardens.Received(1).ChangeElementStateAsync(entity, Arg.Any<GardenElementState>());
    }

    [Fact]
    public void PlantSpot_SuccessConsumesSelectedSourceAndUpdatesCell()
    {
        var vm = CreateVm();
        var spot = new PlantingSpotFromViewModel { Id = 4 };
        var source = new PlantSourceItem { Kind = PlantSourceKind.Seed, EntityId = 7, PlantName = "Томат Черри", AvailableQty = 2 };
        vm.AvailableSeedItems = [source]; vm.SelectedPlantSource = source; vm.PlantingAmount = 2;
        _planting.PlantAsync(Arg.Any<PlantRequest>()).Returns(Result.Success(new PlantingResultFromViewModel(4, 9, SeedAmount.Create(2, SeedUnit.Pieces).ValueOrThrow(), SeedAmount.Create(0, SeedUnit.Pieces).ValueOrThrow())));
        vm.PlantSpotCommand.Execute(spot);
        Assert.Empty(vm.AvailableSeedItems); Assert.Null(vm.SelectedPlantSource);
        Assert.Equal(nameof(PlantedSpotState), spot.StateTypeName);
        Assert.Equal("Томат Черри", spot.PlantLabel); Assert.Equal(9, spot.SeedlingInfoId);
        _planting.Received(1).PlantAsync(Arg.Is<PlantRequest>(r => r.SpotId == 4 && r.EntityId == 7 && r.Amount == 2));
    }

    [Fact]
    public void PlantSpot_Refused_DoesNotConsumeInventory()
    {
        var vm = CreateVm(); var source = new PlantSourceItem { AvailableQty = 3 };
        vm.SelectedPlantSource = source;
        _planting.PlantAsync(Arg.Any<PlantRequest>()).Returns(Result.Failure<PlantingResultFromViewModel>("Нет остатка"));
        var spot = new PlantingSpotFromViewModel { Id = 1 };
        vm.PlantSpotCommand.Execute(spot);
        Assert.Same(source, vm.SelectedPlantSource); Assert.Equal(3, source.AvailableQty);
        Assert.NotEqual(nameof(PlantedSpotState), spot.StateTypeName);
    }

    [Fact]
    public void ClearSpot_ClearsDisplayedPlantAndDate()
    {
        var vm = CreateVm(); var entity = new PlantingSpot { Id = 1 };
        _gardens.GetSpotAsync(1).Returns(entity);
        var spot = new PlantingSpotFromViewModel { Id = 1, PlantLabel = "Томат", PlantedDate = DateTime.Today };
        vm.ClearSpotCommand.Execute(spot);
        Assert.Equal(nameof(EmptySpotState), spot.StateTypeName);
        Assert.Null(spot.PlantLabel); Assert.Null(spot.PlantedDate);
        _gardens.Received(1).ClearSpotAsync(entity);
    }
}
