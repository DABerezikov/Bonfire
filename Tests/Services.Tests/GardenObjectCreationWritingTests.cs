using Bonfire.Application.Garden;
using Bonfire.Domain.Garden;
using Bonfire.Domain.Garden.States;
using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Planting;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class GardenObjectCreationWritingTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();
    private CreateGardenObjects Create() => new(new DbGardenObjectCreationWriter(_fixture.Factory));
    private async Task<int> PrepareAsync(bool inner)
    {
        var service = _fixture.CreateGardenService();
        var plan = await service.CreatePlanAsync("План", 2026);
        var garden = await service.CreateGardenAsync(plan.Id, "Участок", 20, 15);
        return inner ? (await service.AddGreenhouseAsync(garden.Id, "Теплица", 6, 3)).Id : garden.Id;
    }

    [Theory]
    [InlineData("Bed", false)]
    [InlineData("ColdFrame", false)]
    [InlineData("FlowerBed", true)]
    [InlineData("OpenGroundArea", true)]
    [InlineData("Unknown", false)]
    public async Task CreateElement_EachKind_PreservesDefaultsAndContainer(string code, bool inner)
    {
        var id = await PrepareAsync(inner); var template = GardenElementTemplate.From(code);
        var saved = (await Create().CreateElementAsync(new(id, "  Имя  ", template.Kind, 15, 15, template.WidthMeters * 150, template.HeightMeters * 150))).ValueOrThrow();
        var row = await _fixture.QueryAsync(db => db.GardenElements.SingleAsync());
        Assert.Equal(saved.Properties.Id, row.Id); Assert.Equal(id, row.PlotId); Assert.Equal("  Имя  ", row.Name);
        Assert.Equal(saved.ElementType, row.GetType().Name); Assert.Equal(15, row.X); Assert.Equal(15, row.Y);
        Assert.Equal(template.WidthMeters * 150, row.DisplayWidth); Assert.Equal(template.HeightMeters * 150, row.DisplayHeight);
        Assert.Equal(nameof(PlannedState), row.StateTypeName); Assert.False(row.IsLocked); Assert.Equal(0, row.Rotation);
        Assert.Equal(0, row.AreaSquareMeters); Assert.Equal(1, row.GridRows); Assert.Equal(1, row.GridColumns);
        Assert.Null(row.Note); Assert.Null(row.SoilType); Assert.Empty(row.PlantingSpots);
        Assert.Equal(inner ? 900 : 3000, saved.Container.Width);
    }

    [Fact]
    public async Task CreateGreenhouse_PreservesInnerOuterDimensionsAndEmptyContent()
    {
        var id = await PrepareAsync(false);
        var saved = (await Create().CreateGreenhouseAsync(new(id, "Теплица 1", 6, 3, 150, 15, 15))).ValueOrThrow();
        var row = await _fixture.QueryAsync(db => db.Greenhouses.SingleAsync());
        Assert.Equal(saved.Properties.Id, row.Id); Assert.Equal(id, row.ParentPlotId);
        Assert.Equal(900, row.DisplayWidth); Assert.Equal(450, row.DisplayHeight); Assert.Equal(900, row.CanvasWidth); Assert.Equal(450, row.CanvasHeight);
        Assert.Equal(6, row.WidthMeters); Assert.Equal(3, row.HeightMeters); Assert.Equal(15, row.X); Assert.Equal(15, row.Y);
        Assert.Equal(nameof(PlannedState), row.StateTypeName); Assert.False(row.IsLocked); Assert.Null(row.Material); Assert.Null(row.Note);
        Assert.Equal(0, await _fixture.QueryAsync(db => db.GardenElements.CountAsync()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_FreshObstacle_RefusesWithoutPartialRow(bool greenhouse)
    {
        var id = await PrepareAsync(false);
        await _fixture.CreateGardenService().GetGardenByIdAsync(id);
        await _fixture.ExecuteAsync(async db => { db.GardenElements.Add(new Bed { PlotId = id, DisplayWidth = 1000, DisplayHeight = 1000 }); await db.SaveChangesAsync(); });
        var error = greenhouse
            ? (await Create().CreateGreenhouseAsync(new(id, "Теплица", 6, 3, 150, 0, 0))).Match(_ => "", e => e)
            : (await Create().CreateElementAsync(new(id, "Элемент", new GardenElementKind.Bed(), 0, 0, 300, 135))).Match(_ => "", e => e);
        Assert.Contains("пересекается", error);
        Assert.Equal(1, await _fixture.QueryAsync(db => db.GardenElements.CountAsync()));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Greenhouses.CountAsync()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_DeletedContainer_Refuses(bool greenhouse)
    {
        var id = await PrepareAsync(false); await _fixture.CreateGardenService().DeleteGardenAsync(new Garden { Id = id });
        var error = greenhouse
            ? (await Create().CreateGreenhouseAsync(new(id, "Теплица", 6, 3, 150, 0, 0))).Match(_ => "", e => e)
            : (await Create().CreateElementAsync(new(id, "Элемент", new GardenElementKind.Bed(), 0, 0, 300, 135))).Match(_ => "", e => e);
        Assert.Contains("Контейнер", error);
        Assert.Equal(0, await _fixture.QueryAsync(db => db.GardenElements.CountAsync()));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Greenhouses.CountAsync()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_DatabaseRejectsWrite_PreservesContainer(bool greenhouse)
    {
        var id = await PrepareAsync(false);
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(greenhouse
            ? "CREATE TRIGGER RejectObject BEFORE INSERT ON GardenPlots BEGIN SELECT RAISE(ABORT, 'object rejected'); END;"
            : "CREATE TRIGGER RejectObject BEFORE INSERT ON GardenElements BEGIN SELECT RAISE(ABORT, 'object rejected'); END;"));
        var error = greenhouse
            ? (await Create().CreateGreenhouseAsync(new(id, "Теплица", 6, 3, 150, 0, 0))).Match(_ => "", e => e)
            : (await Create().CreateElementAsync(new(id, "Элемент", new GardenElementKind.Bed(), 0, 0, 300, 135))).Match(_ => "", e => e);
        Assert.Contains("object rejected", error);
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Gardens.CountAsync()));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.GardenElements.CountAsync()));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Greenhouses.CountAsync()));
    }
}
