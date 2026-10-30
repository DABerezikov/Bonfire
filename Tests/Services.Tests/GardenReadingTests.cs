using Bonfire.Domain.Garden;
using Bonfire.Domain.Functional;
using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Planting;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class GardenReadingTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task Read_SavedPlan_PreservesSubtypesOptionalValuesAndOrderedCells()
    {
        var plan = await _fixture.PreparePlanAsync("  План  ", 2026, "");
        var other = await _fixture.PreparePlanAsync("Другой", 2027);
        await _fixture.ExecuteAsync(async db =>
        {
            db.Gardens.Add(new Garden { Name = "Участок", GardenPlanId = plan.Id, Address = "", Note = null,
                CanvasWidth = 1000, CanvasHeight = 700, WidthMeters = 7, HeightMeters = 5,
                Elements = [new FlowerBed { Name = "Цветник", Shape = "Круг", Rotation = 45, IsLocked = true,
                    StateTypeName = "Историческое состояние", GridRows = 2, GridColumns = 2, AreaSquareMeters = 0,
                    PlantingSpots = [new() { Row = 1, Column = 0, Note = "" }, new() { Row = 0, Column = 0, Note = null }] }],
                Greenhouses = [new() { Name = "Теплица", CanvasWidth = 600, CanvasHeight = 300, WidthMeters = 4, HeightMeters = 2,
                    DisplayWidth = 900, DisplayHeight = 450, X = 15, Rotation = 30,
                    Elements = [new ColdFrame { Name = "Парник", CoverMaterial = "Плёнка" }] }] });
            await db.SaveChangesAsync();
        });
        var reader = new DbGardenReader(_fixture.Factory);
        var plans = (await reader.ReadPlansAsync(default)).ValueOrThrow();
        Assert.Equal(new[] { other.Id, plan.Id }, plans.Select(p => p.Id));
        Assert.Equal("  План  ", plans[1].Name); Assert.Equal("", plans[1].Description.ToNullable());
        var garden = Assert.Single((await reader.ReadGardensAsync(GardenPlanId.Create(plan.Id).ValueOrThrow(), default)).ValueOrThrow());
        Assert.Equal("", garden.Properties.Values.Address.ToNullable()); Assert.IsType<Option<string>.None>(garden.Properties.Values.Note);
        Assert.Equal(1000, garden.Properties.Canvas.Width); Assert.Equal(7, garden.Properties.Values.WidthMeters);
        var element = Assert.Single(garden.Elements); Assert.Equal("FlowerBed", element.ElementType); Assert.Equal("Круг", element.Shape.ToNullable());
        Assert.Equal(45, element.Rotation); Assert.True(element.IsLocked); Assert.Equal("Историческое состояние", element.StateTypeName);
        Assert.Equal(0, element.Properties.AreaSquareMeters); Assert.Equal(0, element.Spots[0].Row); Assert.Equal(1, element.Spots[1].Row);
        Assert.IsType<Option<string>.None>(element.Spots[0].Note); Assert.Equal("", element.Spots[1].Note.ToNullable());
        var house = Assert.Single(garden.Greenhouses); Assert.Equal(900, house.Rectangle.Width); Assert.Equal(600, house.Properties.Canvas.Width);
        Assert.Equal("Плёнка", Assert.Single(house.Elements).Properties.Details.CoverMaterial.ToNullable());
    }

    [Fact]
    public async Task Read_InvalidHistoricalGeometry_ReturnsTargetDiagnosticWithoutChangingRow()
    {
        var plan = await _fixture.PreparePlanAsync("План", 2026);
        var garden = await _fixture.PrepareGardenAsync(plan.Id, "Участок", 10, 10);
        var element = await _fixture.PrepareAsync(new Bed { PlotId = garden.Id, DisplayWidth = -1 });
        var result = await new DbGardenReader(_fixture.Factory).ReadGardensAsync(GardenPlanId.Create(plan.Id).ValueOrThrow(), default);
        Assert.Contains($"{element.Id}", result.Match(_ => "", e => e));
        Assert.Contains("Элемент", result.Match(_ => "", e => e));
        Assert.Equal(-1, await _fixture.QueryAsync(db => db.GardenElements.Select(e => e.DisplayWidth).SingleAsync()));
    }
}
