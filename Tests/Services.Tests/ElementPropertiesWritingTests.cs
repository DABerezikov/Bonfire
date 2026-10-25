using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Domain.Garden.States;
using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Planting;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class ElementPropertiesWritingTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    private Task<ElementEditorValues> PrepareAsync(bool coldFrame = false) => _fixture.QueryAsync(async db =>
    {
        var garden = new Garden { Name = "Участок", GardenPlan = new() { Name = "План", Year = 2026 } };
        GardenElement element = coldFrame ? new ColdFrame { CoverMaterial = "Стекло" } : new Bed { Orientation = "Север" };
        element.Id = 50;
        element.Plot = garden;
        element.Name = "Грядка";
        element.Note = "Исходная";
        element.DisplayWidth = 90;
        element.DisplayHeight = 90;
        element.AreaSquareMeters = 0.4;
        element.Rotation = 45;
        element.StateTypeName = nameof(ActiveState);
        element.PlantingSpots = [new() { Note = "Ячейка", SeedlingInfo = new() { Note = "История" } }];
        db.GardenElements.Add(element);
        db.Greenhouses.Add(new() { Id = 50, ParentPlot = garden, Name = "Теплица", X = 400, DisplayWidth = 200, DisplayHeight = 150 });
        await db.SaveChangesAsync();
        return new ElementEditorValues(new(element.Name, element.Note.ToOption(), element.SoilType.ToOption(),
            element is Bed bed ? bed.Orientation.ToOption() : Option.None<string>(),
            element is ColdFrame frame ? frame.CoverMaterial.ToOption() : Option.None<string>()), 0, 0, 90, 90);
    });

    private Task<Result<ElementPropertiesResultFromViewModel>> SaveAsync(ElementEditorValues original, ElementEditorValues values, int id = 50)
        => _fixture.CreateGardenService().EditElementPropertiesAsync(new(id, original, values, 150));

    [Fact]
    public async Task Edit_MetadataOnly_PreservesFreshGeometryAreaLockStateAndUntouchedNote()
    {
        var original = await PrepareAsync();
        await _fixture.ExecuteAsync(async db =>
        {
            var element = await db.GardenElements.SingleAsync();
            element.X = 30; element.Y = 45; element.DisplayWidth = 180; element.AreaSquareMeters = 0.7;
            element.Note = "Свежая"; element.IsLocked = true;
            await db.SaveChangesAsync();
        });
        var saved = (await SaveAsync(original, original with { Details = original.Details with { Name = "  Новое  " } })).ValueOrThrow();
        Assert.Equal((30.0, 45.0, 180.0, 0.7), (saved.Rectangle.X, saved.Rectangle.Y, saved.Rectangle.Width, saved.AreaSquareMeters));
        Assert.Equal("Свежая", saved.Details.Note.ToNullable());
        Assert.Equal("  Новое  ", saved.Details.Name);
        var stored = await _fixture.QueryAsync(db => db.GardenElements.SingleAsync());
        Assert.True(stored.IsLocked);
        Assert.Equal((45.0, nameof(ActiveState), 1, 1), (stored.Rotation, stored.StateTypeName, stored.GridRows, stored.GridColumns));
        Assert.Equal("Ячейка", await _fixture.QueryAsync(db => db.PlantingSpots.Select(s => s.Note).SingleAsync()));
        Assert.Equal("История", await _fixture.QueryAsync(db => db.SeedlingInfos.Select(s => s.Note).SingleAsync()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Edit_SubtypeAndOptionalValues_PreservesExactTextAndNull(bool coldFrame)
    {
        var original = await PrepareAsync(coldFrame);
        var values = original with { Details = original.Details with { Name = "", Note = Option.None<string>(), SoilType = Option.Some(""),
            Orientation = Option.Some("  Юг  "), CoverMaterial = Option.Some("  Плёнка  ") } };
        var result = (await SaveAsync(original, values)).ValueOrThrow();
        Assert.IsType<Option<string>.None>(result.Details.Note);
        Assert.Equal("", result.Details.Name);
        Assert.Equal("", result.Details.SoilType.ToNullable());
        if (coldFrame) Assert.Equal("  Плёнка  ", await _fixture.QueryAsync(db => db.GardenElements.OfType<ColdFrame>().Select(e => e.CoverMaterial).SingleAsync()));
        else Assert.Equal("  Юг  ", await _fixture.QueryAsync(db => db.GardenElements.OfType<Bed>().Select(e => e.Orientation).SingleAsync()));
    }

    [Fact]
    public async Task Edit_WidthOnly_MergesFreshPositionAndSavesGeometryAndArea()
    {
        var original = await PrepareAsync();
        await _fixture.ExecuteAsync(async db => { (await db.GardenElements.SingleAsync()).X = 30; await db.SaveChangesAsync(); });
        var saved = (await SaveAsync(original, original with { Width = 180 })).ValueOrThrow();
        Assert.Equal((30.0, 180.0, 0.7), (saved.Rectangle.X, saved.Rectangle.Width, saved.AreaSquareMeters));
        var stored = await _fixture.QueryAsync(db => db.GardenElements.SingleAsync());
        Assert.Equal((30.0, 180.0, 0.7), (stored.X, stored.DisplayWidth, stored.AreaSquareMeters));
    }

    [Fact]
    public async Task Edit_GeometryLockedAfterOpening_RefusesMetadataToo()
    {
        var original = await PrepareAsync();
        await _fixture.ExecuteAsync(async db => { (await db.GardenElements.SingleAsync()).IsLocked = true; await db.SaveChangesAsync(); });
        var result = await SaveAsync(original, original with { X = 30, Details = original.Details with { Name = "Не сохранять" } });
        Assert.Contains("заблокирована", result.Match(_ => "", error => error));
        Assert.Equal("Грядка", await _fixture.QueryAsync(db => db.GardenElements.Select(e => e.Name).SingleAsync()));
    }

    [Fact]
    public async Task Edit_CollisionWithSameIdInOtherTable_RefusesEntireEdit()
    {
        var original = await PrepareAsync();
        var result = await SaveAsync(original, original with { X = 450, Details = original.Details with { Name = "Не сохранять" } });
        Assert.Contains("пересекается", result.Match(_ => "", error => error));
        Assert.Equal("Грядка", await _fixture.QueryAsync(db => db.GardenElements.Select(e => e.Name).SingleAsync()));
    }

    [Fact]
    public async Task Edit_SaveFailure_RollsBackMetadataGeometryAndArea()
    {
        var original = await PrepareAsync();
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectProperties BEFORE UPDATE ON GardenElements BEGIN SELECT RAISE(ABORT, 'properties rejected'); END;"));
        var result = await SaveAsync(original, original with { Width = 180, Details = original.Details with { Name = "Не сохранять" } });
        Assert.Contains("properties rejected", result.Match(_ => "", error => error));
        var stored = await _fixture.QueryAsync(db => db.GardenElements.SingleAsync());
        Assert.Equal(("Грядка", 90.0, 0.4), (stored.Name, stored.DisplayWidth, stored.AreaSquareMeters));
    }

    [Fact]
    public async Task Edit_MissingObject_ReturnsFailure()
    {
        var original = await PrepareAsync();
        Assert.True((await SaveAsync(original, original, 999)).IsFailure);
    }
}
