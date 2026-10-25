using BonfireDB;
using BonfireDB.Context;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Bonfire.Application.Garden;
using BonfireDB.Planting;

namespace Services.Tests;

// Интеграционные тесты GardenService на реальном EF (InMemory) + UoW.
// Проверяют read-методы, перенесённые из ViewModel в сервис, на коротком контексте.
public class GardenServiceIntegrationTests : IDisposable
{
    private readonly ServiceProvider _sp;
    private readonly DbBonfire _db;

    public GardenServiceIntegrationTests()
    {
        // Фиксированное имя БД: все области (scopes) UoW делят один InMemory-стор.
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<DbBonfire>(o => o.UseInMemoryDatabase(dbName));
        services.AddRepositoriesInDb();
        _sp = services.BuildServiceProvider();
        _db = _sp.GetRequiredService<DbBonfire>();
    }

    public void Dispose() => _sp.Dispose();

    private GardenService CreateService()
    {
        var factory = _sp.GetRequiredService<IUnitOfWorkFactory>();
        return new(factory, new ResizeGrid(new DbGridWriter(factory)), new ChangeGardenState(new DbGardenStateWriter(factory)), new SaveGardenGeometry(new DbGardenGeometryWriter(factory)), new EditElementProperties(new DbElementPropertiesWriter(factory)), new EditGreenhouseProperties(new DbGreenhousePropertiesWriter(factory)), new EditGardenProperties(new DbGardenPropertiesWriter(factory)), new SetGardenLock(new DbGardenLockWriter(factory)), new DeleteGardenElement(new DbElementDeletionWriter(factory)), new DeleteGreenhouse(new DbGreenhouseDeletionWriter(factory)), new DeleteGarden(new DbGardenDeletionWriter(factory)), new DeleteGardenPlan(new DbGardenPlanDeletionWriter(factory)), new CreateGardenPlan(new DbGardenPlanCreationWriter(factory), TimeProvider.System), new CreateGarden(new DbGardenCreationWriter(factory)), new CreateGardenObjects(new DbGardenObjectCreationWriter(factory)));
    }

    private async Task<(int planId, int gardenId, int bedId)> SeedAsync(int year = 2026)
    {
        var plan = new GardenPlan { Name = $"План {year}", Year = year };
        _db.GardenPlans.Add(plan);
        await _db.SaveChangesAsync();

        var garden = new Garden
        {
            GardenPlanId = plan.Id, Name = "Участок",
            WidthMeters = 10, HeightMeters = 5, CanvasWidth = 400, CanvasHeight = 200
        };
        _db.Gardens.Add(garden);
        await _db.SaveChangesAsync();

        var bed = new Bed { PlotId = garden.Id, Name = "Грядка 1" };
        bed.PlantingSpots.Add(new PlantingSpot { Row = 0, Column = 0 });
        _db.GardenElements.Add(bed);
        await _db.SaveChangesAsync();

        return (plan.Id, garden.Id, bed.Id);
    }

    [Fact]
    public async Task GetPlansOrderedByYearDescAsync_ReturnsPlansNewestFirst()
    {
        await SeedAsync(2024);
        await SeedAsync(2026);
        await SeedAsync(2025);

        var plans = await CreateService().GetPlansOrderedByYearDescAsync();

        Assert.Equal([2026, 2025, 2024], plans.Select(p => p.Year));
    }

    [Fact]
    public async Task GetGardensByPlanAsync_ReturnsGardensOfThatPlanOnly()
    {
        var (planId, gardenId, _) = await SeedAsync();
        await SeedAsync(2030); // другой план — не должен попасть

        var gardens = await CreateService().GetGardensByPlanAsync(planId);

        Assert.Single(gardens);
        Assert.Equal(gardenId, gardens[0].Id);
    }

    [Fact]
    public async Task GetGardenByIdAsync_ReturnsGardenWithElements()
    {
        var (_, gardenId, bedId) = await SeedAsync();

        var garden = await CreateService().GetGardenByIdAsync(gardenId);

        Assert.NotNull(garden);
        Assert.Equal(gardenId, garden!.Id);
        Assert.Contains(garden.Elements, e => e.Id == bedId);
    }

    [Fact]
    public async Task GetGardenByIdAsync_UnknownId_ReturnsNull()
    {
        await SeedAsync();
        var garden = await CreateService().GetGardenByIdAsync(99999);
        Assert.Null(garden);
    }

    [Fact]
    public async Task GetElementByIdAsync_ReturnsElementWithPlantingSpots()
    {
        var (_, _, bedId) = await SeedAsync();

        var element = await CreateService().GetElementByIdAsync(bedId);

        Assert.NotNull(element);
        Assert.IsType<Bed>(element);
        Assert.Single(element!.PlantingSpots);
    }
}
