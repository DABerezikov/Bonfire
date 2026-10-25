using BonfireDB.Entities.GardenPlanning;
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
    public async Task PlansAndGardens_ReadOrderFilterUpdateAndDeleteAcrossContexts()
    {
        var old = await Service.CreatePlanAsync("Старый", 2025);
        var current = await Service.CreatePlanAsync("Текущий", 2026);
        var garden = await Service.CreateGardenAsync(current.Id, "Участок", 10, 5);
        await Service.CreateGardenAsync(old.Id, "Другой", 5, 5);
        Assert.Equal(new[] { 2026, 2025 }, (await Service.GetPlansOrderedByYearDescAsync()).Select(p => p.Year));
        Assert.Equal(garden.Id, Assert.Single(await Service.GetGardensByPlanAsync(current.Id)).Id);
        current.Name = "Обновлённый"; await Service.UpdatePlanAsync(current);
        Assert.Equal("Обновлённый", (await Service.GetPlansOrderedByYearDescAsync())[0].Name);
        var detached = (await Service.GetGardenByIdAsync(garden.Id))!;
        detached.Address = "Адрес"; detached.Note = "Заметка";
        await Service.UpdateGardenAsync(detached);
        Assert.Equal("Адрес", (await Service.GetGardenByIdAsync(garden.Id))!.Address);
        await Service.DeleteGardenAsync(detached);
        Assert.Null(await Service.GetGardenByIdAsync(garden.Id));
        await Service.DeletePlanAsync(old);
        Assert.Single(await Service.GetPlansOrderedByYearDescAsync());
    }

    [Theory]
    [InlineData("Bed")]
    [InlineData("ColdFrame")]
    [InlineData("FlowerBed")]
    [InlineData("OpenGroundArea")]
    public async Task ElementLifecycle_PreservesSubtypeStatesGridAndDeletion(string kind)
    {
        var plan = await Service.CreatePlanAsync("План", 2026);
        var garden = await Service.CreateGardenAsync(plan.Id, "Участок", 20, 15);
        GardenElement element = kind switch
        {
            "Bed" => new Bed { Orientation = "Север" },
            "ColdFrame" => new ColdFrame { CoverMaterial = "Плёнка" },
            "FlowerBed" => new FlowerBed { Shape = "Круг" },
            _ => new OpenGroundArea()
        };
        element.PlotId = garden.Id; element.Name = "Элемент";
        await Service.AddElementAsync(element);
        var loaded = (await Service.GetElementByIdAsync(element.Id))!;
        Assert.Equal(kind, loaded.GetType().Name);
        loaded.Note = "Проверка"; await Service.UpdateElementAsync(loaded);
        await Service.RebuildGridAsync(loaded, 2, 2);
        var id = loaded.PlantingSpots.Single(s => s.Row == 0 && s.Column == 0).Id;
        await Service.RebuildGridAsync(loaded, 3, 3);
        Assert.Equal(id, loaded.PlantingSpots.Single(s => s.Row == 0 && s.Column == 0).Id);
        await Service.RebuildGridAsync(loaded, 1, 1);
        Assert.Equal(id, Assert.Single((await Service.GetElementByIdAsync(element.Id))!.PlantingSpots).Id);
        await Service.ChangeElementStateAsync(loaded, new PreparedState());
        await Service.ChangeElementStateAsync(loaded, new ActiveState());
        Assert.Equal(nameof(ActiveState), (await Service.GetElementByIdAsync(element.Id))!.StateTypeName);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service.RebuildGridAsync(loaded, 2, 2));
        await Service.DeleteElementAsync(loaded);
        Assert.Null(await Service.GetElementByIdAsync(element.Id));
        Assert.Equal(0, await _fx.QueryAsync(db => db.PlantingSpots.CountAsync()));
    }

    [Fact]
    public async Task GreenhouseLifecycle_PersistsStateMetadataAndDeletion()
    {
        var plan = await Service.CreatePlanAsync("План", 2026);
        var garden = await Service.CreateGardenAsync(plan.Id, "Участок", 20, 15);
        var greenhouse = await Service.AddGreenhouseAsync(garden.Id, "Теплица", 6, 3);
        greenhouse.Note = "Заметка"; greenhouse.Material = "Стекло";
        await Service.UpdateGreenhouseAsync(greenhouse);
        await Service.ChangeGreenhouseStateAsync(greenhouse, new PreparedState());
        var loaded = (await Service.GetGreenhouseByIdAsync(greenhouse.Id))!;
        Assert.Equal("Стекло", loaded.Material); Assert.Equal(nameof(PreparedState), loaded.StateTypeName);
        await Service.DeleteGreenhouseAsync(loaded);
        Assert.Null(await Service.GetGreenhouseByIdAsync(greenhouse.Id));
    }

    [Fact]
    public async Task SpotLifecycle_PersistsReservationPlantingHarvestAndClear()
    {
        var plan = await Service.CreatePlanAsync("План", 2026);
        var garden = await Service.CreateGardenAsync(plan.Id, "Участок", 10, 10);
        var bed = await Service.AddElementAsync(new Bed { PlotId = garden.Id });
        await Service.RebuildGridAsync(bed, 1, 1);
        var spot = Assert.Single(bed.PlantingSpots);
        await Service.ChangeSpotStateAsync(spot, new ReservedSpotState(), plantLabel: "Томат");
        Assert.Equal("Томат", (await Service.GetSpotAsync(spot.Id))!.Note);
        await Service.ChangeSpotStateAsync(spot, new PlantedSpotState(), plantedDate: new DateTime(2026, 5, 1));
        await Service.ChangeSpotStateAsync(spot, new HarvestedSpotState());
        spot.HarvestDate = new DateTime(2026, 8, 1); await Service.UpdateSpotAsync(spot);
        Assert.Equal(new DateTime(2026, 8, 1), (await Service.GetSpotAsync(spot.Id))!.HarvestDate);
        await Service.ClearSpotAsync(spot);
        var cleared = (await Service.GetSpotAsync(spot.Id))!;
        Assert.Equal(nameof(EmptySpotState), cleared.StateTypeName);
        Assert.Null(cleared.PlantedDate); Assert.Null(cleared.HarvestDate); Assert.Null(cleared.SeedlingInfoId);
    }
}
