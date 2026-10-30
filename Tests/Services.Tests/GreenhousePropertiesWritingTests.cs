using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Domain.Garden.States;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class GreenhousePropertiesWritingTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    private Task<GreenhouseEditorValues> PrepareAsync() => _fixture.QueryAsync(async db =>
    {
        var house = new Greenhouse { Id = 50, Name = "Теплица", Note = "Исходная", Material = "Стекло", WidthMeters = 6, HeightMeters = 3,
            CanvasWidth = 900, CanvasHeight = 450, X = 30, Y = 45, DisplayWidth = 200, DisplayHeight = 150, Rotation = 30,
            IsLocked = true, StateTypeName = nameof(ActiveState),
            ParentPlot = new Garden { Name = "Участок", GardenPlan = new() { Name = "План", Year = 2026 } },
            Elements = [new Bed { Name = "Внутри", X = 600, DisplayWidth = 90, DisplayHeight = 90,
                PlantingSpots = [new() { Note = "Ячейка", SeedlingInfo = new() { Note = "История" } }] }],
            Greenhouses = [new() { Name = "Вложенная", X = 50000 }] };
        db.Greenhouses.Add(house);
        await db.SaveChangesAsync();
        return new GreenhouseEditorValues(house.Name, house.Note.ToOption(), house.Material.ToOption(), house.WidthMeters, house.HeightMeters);
    });

    private Task<Result<GreenhousePropertiesResultFromViewModel>> SaveAsync(GreenhouseEditorValues original, GreenhouseEditorValues values, int id = 50)
        => _fixture.CreateGardenService().EditGreenhousePropertiesAsync(new(id, original, values, 150));

    [Fact]
    public async Task Edit_MetadataOnly_PreservesFreshDimensionsCanvasAndUntouchedFields()
    {
        var original = await PrepareAsync();
        await _fixture.ExecuteAsync(async db =>
        {
            var house = await db.Greenhouses.SingleAsync(g => g.Id == 50);
            house.WidthMeters = 8; house.HeightMeters = 4; house.CanvasWidth = 1234; house.CanvasHeight = 600;
            house.Note = "Свежая"; house.X = 90; house.DisplayWidth = 250;
            await db.SaveChangesAsync();
        });
        var result = (await SaveAsync(original, original with { Name = "  Новое  " })).ValueOrThrow();
        Assert.Equal((8.0, 4.0, 1234.0, 600.0), (result.Values.WidthMeters, result.Values.HeightMeters, result.Canvas.Width, result.Canvas.Height));
        Assert.Equal("Свежая", result.Values.Note.ToNullable());
        var stored = await _fixture.QueryAsync(db => db.Greenhouses.SingleAsync(g => g.Id == 50));
        Assert.Equal(("  Новое  ", 90.0, 250.0, 30.0, nameof(ActiveState)), (stored.Name, stored.X, stored.DisplayWidth, stored.Rotation, stored.StateTypeName));
        Assert.True(stored.IsLocked);
    }

    [Fact]
    public async Task Edit_WidthOnly_UsesFreshHeightAndAllowsLockedHouseWithoutChangingComposite()
    {
        var original = await PrepareAsync();
        await _fixture.ExecuteAsync(async db => { var house = await db.Greenhouses.SingleAsync(g => g.Id == 50); house.HeightMeters = 4; house.CanvasHeight = 600; await db.SaveChangesAsync(); });
        var result = (await SaveAsync(original, original with { WidthMeters = 5 })).ValueOrThrow();
        Assert.Equal((5.0, 4.0, 750.0, 600.0), (result.Values.WidthMeters, result.Values.HeightMeters, result.Canvas.Width, result.Canvas.Height));
        var stored = await _fixture.QueryAsync(db => db.Greenhouses.Include(g => g.Elements).Include(g => g.Greenhouses).SingleAsync(g => g.Id == 50));
        Assert.True(stored.IsLocked);
        Assert.Equal((30.0, 45.0, 200.0, 150.0, 30.0), (stored.X, stored.Y, stored.DisplayWidth, stored.DisplayHeight, stored.Rotation));
        Assert.Equal(750, stored.CanvasWidth);
        Assert.Single(stored.Elements);
        Assert.Equal(50000, Assert.Single(stored.Greenhouses).X);
        Assert.Equal("Ячейка", await _fixture.QueryAsync(db => db.PlantingSpots.Select(s => s.Note).SingleAsync()));
        Assert.Equal("История", await _fixture.QueryAsync(db => db.SeedlingInfos.Select(s => s.Note).SingleAsync()));
    }

    [Fact]
    public async Task Edit_ChildAddedAfterSnapshot_RefusesWholeEditUsingFreshContents()
    {
        var original = await PrepareAsync();
        await _fixture.ExecuteAsync(async db =>
        {
            db.GardenElements.Add(new Bed { PlotId = 50, Name = "Новый элемент", X = 740, DisplayWidth = 90, DisplayHeight = 90 });
            await db.SaveChangesAsync();
        });
        var result = await SaveAsync(original, original with { WidthMeters = 5, Name = "Не сохранять" });
        Assert.Contains("Новый элемент", result.Match(_ => "", error => error));
        var stored = await _fixture.QueryAsync(db => db.Greenhouses.SingleAsync(g => g.Id == 50));
        Assert.Equal(("Теплица", 6.0, 900.0), (stored.Name, stored.WidthMeters, stored.CanvasWidth));
    }

    [Fact]
    public async Task Edit_InvalidFreshChildGeometry_RefusesInsteadOfSkipping()
    {
        var original = await PrepareAsync();
        await _fixture.ExecuteAsync(async db => { (await db.GardenElements.SingleAsync()).DisplayWidth = -1; await db.SaveChangesAsync(); });
        Assert.True((await SaveAsync(original, original with { Name = "Не сохранять" })).IsFailure);
    }

    [Fact]
    public async Task Edit_OptionalValues_PreservesEmptyWhitespaceAndNull()
    {
        var original = await PrepareAsync();
        var result = (await SaveAsync(original, original with { Name = "", Note = Option.None<string>(), Material = Option.Some("") })).ValueOrThrow();
        Assert.Equal("", result.Values.Name);
        Assert.IsType<Option<string>.None>(result.Values.Note);
        Assert.Equal("", result.Values.Material.ToNullable());
        var stored = await _fixture.QueryAsync(db => db.Greenhouses.SingleAsync(g => g.Id == 50));
        Assert.Null(stored.Note);
        Assert.Equal("", stored.Material);
    }

    [Fact]
    public async Task Edit_EmptyHouse_ZeroSizeAllowed()
    {
        var original = await PrepareAsync();
        await _fixture.ExecuteAsync(async db => { db.GardenElements.RemoveRange(db.GardenElements); await db.SaveChangesAsync(); });
        var result = (await SaveAsync(original, original with { WidthMeters = 0, HeightMeters = 0 })).ValueOrThrow();
        Assert.Equal((0.0, 0.0), (result.Canvas.Width, result.Canvas.Height));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
    }

    [Fact]
    public async Task Edit_DatabaseRejectsUpdate_RollsBackPropertiesAndDimensions()
    {
        var original = await PrepareAsync();
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectGreenhouse BEFORE UPDATE ON GardenPlots BEGIN SELECT RAISE(ABORT, 'greenhouse rejected'); END;"));
        var result = await SaveAsync(original, original with { Name = "Не сохранять", WidthMeters = 5 });
        Assert.Contains("greenhouse rejected", result.Match(_ => "", error => error));
        var stored = await _fixture.QueryAsync(db => db.Greenhouses.SingleAsync(g => g.Id == 50));
        Assert.Equal(("Теплица", 6.0, 900.0), (stored.Name, stored.WidthMeters, stored.CanvasWidth));
    }

    [Fact]
    public async Task Edit_MissingHouse_ReturnsFailure()
    {
        var original = await PrepareAsync();
        Assert.True((await SaveAsync(original, original, 999)).IsFailure);
    }
}
