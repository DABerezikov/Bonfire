using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class GardenPropertiesWritingTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    private Task<GardenEditorValues> PrepareAsync() => _fixture.QueryAsync(async db =>
    {
        var garden = new Garden { Id = 50, Name = "Участок", Note = "Исходная", Address = "Адрес", WidthMeters = 6, HeightMeters = 3,
            CanvasWidth = 900, CanvasHeight = 450,
            GardenPlan = new() { Name = "План", Year = 2026 },
            Elements = [new Bed { Name = "Внутри", X = 600, DisplayWidth = 90, DisplayHeight = 90,
                PlantingSpots = [new() { Note = "Ячейка", SeedlingInfo = new() { Note = "История" } }] }],
            Greenhouses = [new() { Name = "Снаружи", X = 600, DisplayWidth = 90, DisplayHeight = 90,
                Elements = [new Bed { Name = "Внутренняя", X = 50000 }] }] };
        db.Gardens.Add(garden);
        await db.SaveChangesAsync();
        return new GardenEditorValues(garden.Name, garden.Note.ToOption(), garden.Address.ToOption(), garden.WidthMeters, garden.HeightMeters);
    });

    private Task<Result<GardenPropertiesResultFromViewModel>> SaveAsync(GardenEditorValues original, GardenEditorValues values, int id = 50)
        => _fixture.CreateGardenService().EditGardenPropertiesAsync(new(id, original, values, 150));

    [Fact]
    public async Task Edit_MetadataOnly_PreservesFreshDimensionsCanvasAndUntouchedFields()
    {
        var original = await PrepareAsync();
        await _fixture.ExecuteAsync(async db =>
        {
            var garden = await db.Gardens.SingleAsync(g => g.Id == 50);
            garden.WidthMeters = 8; garden.HeightMeters = 4; garden.CanvasWidth = 1234; garden.CanvasHeight = 600;
            garden.Note = "Свежая"; garden.Address = "Свежий адрес";
            await db.SaveChangesAsync();
        });
        var result = (await SaveAsync(original, original with { Name = "  Новое  " })).ValueOrThrow();
        Assert.Equal((8.0, 4.0, 1234.0, 600.0), (result.Values.WidthMeters, result.Values.HeightMeters, result.Canvas.Width, result.Canvas.Height));
        Assert.Equal("Свежая", result.Values.Note.ToNullable());
        var stored = await _fixture.QueryAsync(db => db.Gardens.SingleAsync(g => g.Id == 50));
        Assert.Equal(("  Новое  ", "Свежий адрес"), (stored.Name, stored.Address));
    }

    [Fact]
    public async Task Edit_WidthOnly_UsesFreshHeightWithoutChangingChildrenOrHistory()
    {
        var original = await PrepareAsync();
        await _fixture.ExecuteAsync(async db => { var garden = await db.Gardens.SingleAsync(g => g.Id == 50); garden.HeightMeters = 4; garden.CanvasHeight = 600; await db.SaveChangesAsync(); });
        var result = (await SaveAsync(original, original with { WidthMeters = 5 })).ValueOrThrow();
        Assert.Equal((5.0, 4.0, 750.0, 600.0), (result.Values.WidthMeters, result.Values.HeightMeters, result.Canvas.Width, result.Canvas.Height));
        var stored = await _fixture.QueryAsync(db => db.Gardens.Include(g => g.Elements).Include(g => g.Greenhouses).SingleAsync(g => g.Id == 50));
        Assert.Equal(750, stored.CanvasWidth);
        Assert.Single(stored.Elements);
        Assert.Equal(600, Assert.Single(stored.Greenhouses).X);
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
        var stored = await _fixture.QueryAsync(db => db.Gardens.SingleAsync(g => g.Id == 50));
        Assert.Equal(("Участок", 6.0, 900.0), (stored.Name, stored.WidthMeters, stored.CanvasWidth));
    }

    [Fact]
    public async Task Edit_GreenhouseMovedAfterSnapshot_RefusesWholeEditUsingFreshExteriorBounds()
    {
        var original = await PrepareAsync();
        await _fixture.ExecuteAsync(async db => { (await db.Greenhouses.SingleAsync()).X = 740; await db.SaveChangesAsync(); });
        var result = await SaveAsync(original, original with { WidthMeters = 5, Address = Option.Some("Не сохранять") });
        Assert.Contains("теплица «Снаружи»", result.Match(_ => "", error => error));
        var stored = await _fixture.QueryAsync(db => db.Gardens.SingleAsync(g => g.Id == 50));
        Assert.Equal(("Адрес", 6.0, 900.0), (stored.Address, stored.WidthMeters, stored.CanvasWidth));
    }

    [Fact]
    public async Task Edit_InvalidFreshChildGeometry_RefusesInsteadOfSkipping()
    {
        var original = await PrepareAsync();
        await _fixture.ExecuteAsync(async db => { (await db.GardenElements.SingleAsync(e => e.Name == "Внутри")).DisplayWidth = -1; await db.SaveChangesAsync(); });
        Assert.True((await SaveAsync(original, original with { Name = "Не сохранять" })).IsFailure);
    }

    [Fact]
    public async Task Edit_InvalidExteriorGreenhouseGeometry_RefusesInsteadOfSkipping()
    {
        var original = await PrepareAsync();
        await _fixture.ExecuteAsync(async db => { (await db.Greenhouses.SingleAsync()).DisplayHeight = -1; await db.SaveChangesAsync(); });
        Assert.True((await SaveAsync(original, original with { Name = "Не сохранять" })).IsFailure);
        Assert.Equal("Участок", await _fixture.QueryAsync(db => db.Gardens.Select(g => g.Name).SingleAsync()));
    }

    [Fact]
    public async Task Edit_OptionalValues_PreservesEmptyWhitespaceAndNull()
    {
        var original = await PrepareAsync();
        var result = (await SaveAsync(original, original with { Name = "", Note = Option.None<string>(), Address = Option.Some("") })).ValueOrThrow();
        Assert.Equal("", result.Values.Name);
        Assert.IsType<Option<string>.None>(result.Values.Note);
        Assert.Equal("", result.Values.Address.ToNullable());
        var stored = await _fixture.QueryAsync(db => db.Gardens.SingleAsync(g => g.Id == 50));
        Assert.Null(stored.Note);
        Assert.Equal("", stored.Address);
    }

    [Fact]
    public async Task Edit_EmptyGarden_ZeroSizeAllowed()
    {
        var original = await PrepareAsync();
        await _fixture.ExecuteAsync(async db => { db.GardenElements.RemoveRange(db.GardenElements); db.Greenhouses.RemoveRange(db.Greenhouses); await db.SaveChangesAsync(); });
        var result = (await SaveAsync(original, original with { WidthMeters = 0, HeightMeters = 0 })).ValueOrThrow();
        Assert.Equal((0.0, 0.0), (result.Canvas.Width, result.Canvas.Height));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
    }

    [Fact]
    public async Task Edit_DatabaseRejectsUpdate_RollsBackPropertiesAndDimensions()
    {
        var original = await PrepareAsync();
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectGarden BEFORE UPDATE ON GardenPlots BEGIN SELECT RAISE(ABORT, 'garden rejected'); END;"));
        var result = await SaveAsync(original, original with { Name = "Не сохранять", WidthMeters = 5 });
        Assert.Contains("garden rejected", result.Match(_ => "", error => error));
        var stored = await _fixture.QueryAsync(db => db.Gardens.SingleAsync(g => g.Id == 50));
        Assert.Equal(("Участок", 6.0, 900.0), (stored.Name, stored.WidthMeters, stored.CanvasWidth));
    }

    [Fact]
    public async Task Edit_MissingGarden_ReturnsFailure()
    {
        var original = await PrepareAsync();
        Assert.True((await SaveAsync(original, original, 999)).IsFailure);
    }
}
