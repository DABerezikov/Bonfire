using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Domain.Garden.States;
using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Planting;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class GardenGeometryWritingIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    private Task<(int Bed, int House, int Child)> PrepareAsync() => _fixture.QueryAsync(async db =>
    {
        var garden = new Garden { Name = "Участок", GardenPlan = new() { Name = "План", Year = 2026 }, CanvasWidth = 208, CanvasHeight = 208 };
        var house = new Greenhouse { Id = 50, Name = "Теплица", ParentPlot = garden, X = 400, DisplayWidth = 200, DisplayHeight = 150,
            WidthMeters = 6, HeightMeters = 3, CanvasWidth = 900, CanvasHeight = 450, Note = "Сохранить", Material = "Стекло", Rotation = 30,
            Elements = [new Bed { Name = "Внутри", DisplayWidth = 90, DisplayHeight = 90 }],
            Greenhouses = [new() { Name = "Вложенная", X = 500 }] };
        var bed = new Bed { Id = 50, Plot = garden, Name = "Грядка", DisplayWidth = 90, DisplayHeight = 90, AreaSquareMeters = 0.4,
            Note = "История", SoilType = "Суглинок", Orientation = "Север", Rotation = 45, StateTypeName = nameof(ActiveState),
            PlantingSpots = [new() { Note = "Ячейка", SeedlingInfo = new() { Note = "Партия" } }] };
        db.GardenElements.Add(bed);
        db.Greenhouses.Add(house);
        await db.SaveChangesAsync();
        return (bed.Id, house.Id, house.Elements.Single().Id);
    });

    private Task<Result<GeometryResultFromViewModel>> SaveAsync(int id, GeometryTarget target, double x = 120, double width = 90)
        => new SaveGardenGeometry(new DbGardenGeometryWriter(_fixture.Factory)).ExecuteAsync(new(id, target, x, 0, width, 90));

    [Fact]
    public async Task Save_Element_ChangesOnlyGeometryAndAreaPreservingStateAndHistory()
    {
        var ids = await PrepareAsync();
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectProperties BEFORE UPDATE OF Name, Note, SoilType, Orientation ON GardenElements BEGIN SELECT RAISE(ABORT, 'properties must not be updated'); END;"));
        Assert.True((await SaveAsync(ids.Bed, new GeometryTarget.Element(150))).IsSuccess);
        var bed = await _fixture.QueryAsync(db => db.GardenElements.OfType<Bed>().Include(e => e.PlantingSpots).ThenInclude(s => s.SeedlingInfo).SingleAsync(e => e.Id == ids.Bed));
        Assert.Equal((120.0, 0.0, 90.0, 90.0, 0.4), (bed.X, bed.Y, bed.DisplayWidth, bed.DisplayHeight, bed.AreaSquareMeters));
        Assert.Equal(("Грядка", "История", "Суглинок", "Север", 45.0, nameof(ActiveState)), (bed.Name, bed.Note, bed.SoilType, bed.Orientation, bed.Rotation, bed.StateTypeName));
        Assert.Equal("Ячейка", Assert.Single(bed.PlantingSpots).Note);
        Assert.Equal("Партия", bed.PlantingSpots[0].SeedlingInfo!.Note);
    }

    [Fact]
    public async Task Save_Greenhouse_PreservesCompositeAndInternalDimensions()
    {
        var ids = await PrepareAsync();
        Assert.True((await SaveAsync(ids.House, new GeometryTarget.Greenhouse(), 300, 180)).IsSuccess);
        var house = await _fixture.QueryAsync(db => db.Greenhouses.Include(g => g.Elements).Include(g => g.Greenhouses).SingleAsync(g => g.Id == ids.House));
        Assert.Equal((300.0, 180.0, 90.0), (house.X, house.DisplayWidth, house.DisplayHeight));
        Assert.Equal((6.0, 3.0, 900.0, 450.0), (house.WidthMeters, house.HeightMeters, house.CanvasWidth, house.CanvasHeight));
        Assert.Equal(("Теплица", "Сохранить", "Стекло", 30.0), (house.Name, house.Note, house.Material, house.Rotation));
        Assert.Equal(ids.Child, Assert.Single(house.Elements).Id);
        Assert.Equal("Вложенная", Assert.Single(house.Greenhouses).Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_FreshLock_RefusesAndPreservesGeometry(bool greenhouse)
    {
        var ids = await PrepareAsync();
        await _fixture.ExecuteAsync(async db =>
        {
            if (greenhouse) (await db.Greenhouses.SingleAsync(g => g.Id == ids.House)).IsLocked = true;
            else (await db.GardenElements.SingleAsync(e => e.Id == ids.Bed)).IsLocked = true;
            await db.SaveChangesAsync();
        });
        var result = await SaveAsync(greenhouse ? ids.House : ids.Bed, greenhouse ? new GeometryTarget.Greenhouse() : new GeometryTarget.Element(150));
        Assert.Contains("заблокирована", result.Match(_ => "", error => error));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.GardenElements.Where(e => e.Id == ids.Bed).Select(e => e.X).SingleAsync()));
        Assert.Equal(400, await _fixture.QueryAsync(db => db.Greenhouses.Where(g => g.Id == ids.House).Select(g => g.X).SingleAsync()));
    }

    [Fact]
    public async Task Save_FreshCrossTypeCollisionWithSameNumericId_Refuses()
    {
        var ids = await PrepareAsync();
        Assert.Equal(ids.Bed, ids.House);
        var result = await SaveAsync(ids.Bed, new GeometryTarget.Element(150), 450);
        Assert.Contains("пересекается", result.Match(_ => "", error => error));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.GardenElements.Where(e => e.Id == ids.Bed).Select(e => e.X).SingleAsync()));
    }

    [Fact]
    public async Task Save_InitialCollision_AllowsLeavingIt()
    {
        var ids = await PrepareAsync();
        await _fixture.ExecuteAsync(async db =>
        {
            var bed = await db.GardenElements.SingleAsync(e => e.Id == ids.Bed);
            bed.X = 450;
            await db.SaveChangesAsync();
        });
        Assert.True((await SaveAsync(ids.Bed, new GeometryTarget.Element(150), 420)).IsSuccess);
    }

    [Fact]
    public async Task Save_InvalidFreshSiblingGeometry_RefusesWithoutSkippingData()
    {
        var ids = await PrepareAsync();
        await _fixture.ExecuteAsync(async db =>
        {
            (await db.Greenhouses.SingleAsync(g => g.Id == ids.House)).DisplayWidth = -1;
            await db.SaveChangesAsync();
        });
        Assert.True((await SaveAsync(ids.Bed, new GeometryTarget.Element(150))).IsFailure);
        Assert.Equal(0, await _fixture.QueryAsync(db => db.GardenElements.Where(e => e.Id == ids.Bed).Select(e => e.X).SingleAsync()));
    }

    [Fact]
    public async Task Save_ChildInsideLockedGreenhouse_IgnoresParentLockAndObjectsInOtherContainers()
    {
        var ids = await PrepareAsync();
        await _fixture.ExecuteAsync(async db =>
        {
            (await db.Greenhouses.SingleAsync(g => g.Id == ids.House)).IsLocked = true;
            await db.SaveChangesAsync();
        });
        Assert.True((await SaveAsync(ids.Child, new GeometryTarget.Element(150), 30)).IsSuccess);
    }

    [Fact]
    public async Task Save_DatabaseRejectsUpdate_RollsBackAllGeometryAndArea()
    {
        var ids = await PrepareAsync();
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectGeometry BEFORE UPDATE ON GardenElements BEGIN SELECT RAISE(ABORT, 'geometry rejected'); END;"));
        var result = await SaveAsync(ids.Bed, new GeometryTarget.Element(150), 120, 180);
        Assert.Contains("geometry rejected", result.Match(_ => "", error => error));
        var bed = await _fixture.QueryAsync(db => db.GardenElements.SingleAsync(e => e.Id == ids.Bed));
        Assert.Equal((0.0, 90.0, 0.4), (bed.X, bed.DisplayWidth, bed.AreaSquareMeters));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_Missing_ReturnsFailure(bool greenhouse)
        => Assert.True((await SaveAsync(999, greenhouse ? new GeometryTarget.Greenhouse() : new GeometryTarget.Element(150))).IsFailure);
}
