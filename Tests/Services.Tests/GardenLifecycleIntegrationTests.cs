using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Domain.Garden.States;
using Bonfire.Domain.Garden.SpotStates;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class GardenLifecycleIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fx = new();
    private GardenService Service => _fx.CreateGardenService();
    public void Dispose() => _fx.Dispose();

    [Fact]
    public async Task PlansAndGardens_ReadOrderFilterEditAndDeleteAcrossContexts()
    {
        var old = await _fx.PreparePlanAsync("Старый", 2025);
        var current = await _fx.PreparePlanAsync("Текущий", 2026);
        var garden = (await Service.CreateGardenFromRequestAsync(new(current.Id, "Участок", 10, 5, 150))).ValueOrThrow();
        (await Service.CreateGardenFromRequestAsync(new(old.Id, "Другой", 5, 5, 150))).ValueOrThrow();
        Assert.Equal(new[] { 2026, 2025 }, (await Service.ReadPlansAsync()).ValueOrThrow().Select(p => p.Year));
        Assert.Equal(garden.Id, Assert.Single((await Service.ReadGardensAsync(current.Id)).ValueOrThrow()).Properties.Id);
        var edited = garden.Values with { Address = Option.Some("Адрес"), Note = Option.Some("Заметка") };
        Assert.True((await Service.EditGardenPropertiesAsync(new(garden.Id, garden.Values, edited, 150))).IsSuccess);
        Assert.Equal("Адрес", Assert.Single((await Service.ReadGardensAsync(current.Id)).ValueOrThrow()).Properties.Values.Address.ToNullable());
        Assert.True((await Service.DeleteGardenByIdAsync(new(garden.Id))).IsSuccess);
        Assert.Empty((await Service.ReadGardensAsync(current.Id)).ValueOrThrow());
        Assert.True((await Service.DeletePlanByIdAsync(new(old.Id))).IsSuccess);
        Assert.Single((await Service.ReadPlansAsync()).ValueOrThrow());
    }

    [Theory]
    [InlineData("Bed")]
    [InlineData("ColdFrame")]
    [InlineData("FlowerBed")]
    [InlineData("OpenGroundArea")]
    public async Task ElementLifecycle_PreservesSubtypeStatesGridAndDeletion(string kind)
    {
        var plan = await _fx.PreparePlanAsync("План", 2026);
        var garden = (await Service.CreateGardenFromRequestAsync(new(plan.Id, "Участок", 20, 15, 150))).ValueOrThrow();
        var element = (await Service.CreateElementFromRequestAsync(new(garden.Id, "Элемент", GardenElementTemplate.From(kind).Kind, 0, 0, 150, 150))).ValueOrThrow();
        var p = element.Properties; var r = p.Rectangle;
        var original = new ElementEditorValues(p.Details, r.X, r.Y, r.Width, r.Height);
        Assert.True((await Service.EditElementPropertiesAsync(new(p.Id, original, original with
            { Details = p.Details with { Note = Option.Some("Проверка") } }, 150))).IsSuccess);
        var grid = (await Service.ResizeGridAsync(new(p.Id, 2, 2))).ValueOrThrow();
        var id = grid.Spots.Single(s => s.Row == 0 && s.Column == 0).Id;
        grid = (await Service.ResizeGridAsync(new(p.Id, 3, 3))).ValueOrThrow();
        Assert.Equal(id, grid.Spots.Single(s => s.Row == 0 && s.Column == 0).Id);
        grid = (await Service.ResizeGridAsync(new(p.Id, 1, 1))).ValueOrThrow();
        Assert.Equal(id, Assert.Single(grid.Spots).Id);
        Assert.True((await Service.ChangeStateAsync(new(p.Id, new StateChangeTarget.Element(new PreparedState())))).IsSuccess);
        Assert.True((await Service.ChangeStateAsync(new(p.Id, new StateChangeTarget.Element(new ActiveState())))).IsSuccess);
        var loaded = Assert.Single(Assert.Single((await Service.ReadGardensAsync(plan.Id)).ValueOrThrow()).Elements);
        Assert.Equal(kind, loaded.ElementType); Assert.Equal(nameof(ActiveState), loaded.StateTypeName);
        Assert.Equal("Проверка", loaded.Properties.Details.Note.ToNullable());
        Assert.True((await Service.ResizeGridAsync(new(p.Id, 2, 2))).IsFailure);
        Assert.True((await Service.DeleteGardenElementAsync(new(p.Id))).IsSuccess);
        Assert.Empty(Assert.Single((await Service.ReadGardensAsync(plan.Id)).ValueOrThrow()).Elements);
        Assert.Equal(0, await _fx.QueryAsync(db => db.PlantingSpots.CountAsync()));
    }

    [Fact]
    public async Task GreenhouseLifecycle_PersistsStateMetadataAndDeletion()
    {
        var plan = await _fx.PreparePlanAsync("План", 2026);
        var garden = (await Service.CreateGardenFromRequestAsync(new(plan.Id, "Участок", 20, 15, 150))).ValueOrThrow();
        var greenhouse = (await Service.CreateGreenhouseFromRequestAsync(new(garden.Id, "Теплица", 6, 3, 150, 0, 0))).ValueOrThrow();
        var p = greenhouse.Properties;
        Assert.True((await Service.EditGreenhousePropertiesAsync(new(p.Id, p.Values,
            p.Values with { Note = Option.Some("Заметка"), Material = Option.Some("Стекло") }, 150))).IsSuccess);
        Assert.True((await Service.ChangeStateAsync(new(p.Id, new StateChangeTarget.Greenhouse(new PreparedState())))).IsSuccess);
        var loaded = Assert.Single(Assert.Single((await Service.ReadGardensAsync(plan.Id)).ValueOrThrow()).Greenhouses);
        Assert.Equal("Стекло", loaded.Properties.Values.Material.ToNullable()); Assert.Equal(nameof(PreparedState), loaded.StateTypeName);
        Assert.True((await Service.DeleteGreenhouseByIdAsync(new(p.Id))).IsSuccess);
        Assert.Empty(Assert.Single((await Service.ReadGardensAsync(plan.Id)).ValueOrThrow()).Greenhouses);
    }

    [Fact]
    public async Task SpotLifecycle_PreservesHistoryDatesUntilClear()
    {
        var plan = await _fx.PreparePlanAsync("План", 2026);
        var garden = (await Service.CreateGardenFromRequestAsync(new(plan.Id, "Участок", 10, 10, 150))).ValueOrThrow();
        var bed = (await Service.CreateElementFromRequestAsync(new(garden.Id, "Грядка", new GardenElementKind.Bed(), 0, 0, 150, 150))).ValueOrThrow();
        var spot = Assert.Single((await Service.ResizeGridAsync(new(bed.Properties.Id, 1, 1))).ValueOrThrow().Spots);
        var planted = new DateTime(2026, 5, 1); var harvested = new DateTime(2026, 8, 1);
        await _fx.ExecuteAsync(async db =>
        {
            var saved = await db.PlantingSpots.SingleAsync(s => s.Id == spot.Id);
            saved.PlantedDate = planted; saved.HarvestDate = harvested; await db.SaveChangesAsync();
        });
        Assert.True((await Service.ChangeStateAsync(new(spot.Id, new StateChangeTarget.Spot(new SpotChange.Transition(new ReservedSpotState(), Option.Some("Томат")))))).IsSuccess);
        Assert.True((await Service.ChangeStateAsync(new(spot.Id, new StateChangeTarget.Spot(new SpotChange.Transition(new PlantedSpotState(), Option.None<string>()))))).IsSuccess);
        var receipt = (await Service.ChangeStateAsync(new(spot.Id, new StateChangeTarget.Spot(new SpotChange.Transition(new HarvestedSpotState(), Option.None<string>()))))).ValueOrThrow();
        var history = Assert.IsType<GardenStateResultFromViewModel.Spot>(receipt).Cell;
        Assert.Equal(planted, history.PlantedDate.ToNullable()); Assert.Equal(harvested, history.HarvestDate.ToNullable());
        var cleared = Assert.IsType<GardenStateResultFromViewModel.Spot>((await Service.ChangeStateAsync(new(spot.Id, new StateChangeTarget.Spot(new SpotChange.Clear())))).ValueOrThrow()).Cell;
        Assert.Equal(nameof(EmptySpotState), cleared.StateTypeName); Assert.Equal("Томат", cleared.Note.ToNullable());
        Assert.IsType<Option<DateTime>.None>(cleared.PlantedDate); Assert.IsType<Option<DateTime>.None>(cleared.HarvestDate); Assert.IsType<Option<int>.None>(cleared.SeedlingInfoId);
    }
}
