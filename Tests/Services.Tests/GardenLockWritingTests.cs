using Bonfire.Application.Garden;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Planting;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class GardenLockWritingTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    private Task PrepareAsync() => _fixture.ExecuteAsync(async db =>
    {
        var garden = new Garden { Name = "Участок", GardenPlan = new() { Name = "План", Year = 2026 } };
        db.Greenhouses.Add(new Greenhouse { Id = 50, ParentPlot = garden, Name = "Теплица", Note = "Заметка",
            Material = "Стекло", X = 300, Y = 45, Rotation = 30, WidthMeters = 6, HeightMeters = 3,
            CanvasWidth = 900, CanvasHeight = 450, DisplayWidth = 200, DisplayHeight = 150,
            Elements = [new Bed { Name = "Внутри" }], Greenhouses = [new() { Name = "Вложенная" }] });
        db.GardenElements.Add(new Bed { Id = 50, Plot = garden, Name = "Грядка", Note = "Заметка", X = 30, Y = 45,
            DisplayWidth = 150, DisplayHeight = 90, Rotation = 30, AreaSquareMeters = 0.6, SoilType = "Почва",
            Orientation = "Север", GridRows = 2, GridColumns = 3,
            PlantingSpots = [new() { Note = "Ячейка", SeedlingInfo = new() { Note = "История" } }] });
        await db.SaveChangesAsync();
    });

    private Task<Bonfire.Domain.Functional.Result<GardenLockResultFromViewModel>> SaveAsync(bool greenhouse, bool locked, int id = 50)
        => new SetGardenLock(new DbGardenLockWriter(_fixture.Factory)).ExecuteAsync(
            new(id, greenhouse ? new GardenLockTarget.Greenhouse() : new GardenLockTarget.Element(), locked));

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Set_ExplicitState_ChangesOnlyCorrectTypeWithSameId(bool greenhouse, bool locked)
    {
        await PrepareAsync();
        // Свежие поля могли измениться после отображения объекта.
        await _fixture.ExecuteAsync(async db =>
        {
            var bed = await db.GardenElements.OfType<Bed>().SingleAsync(e => e.Id == 50);
            bed.Name = "Свежая грядка"; bed.X = 60; bed.IsLocked = true;
            var house = await db.Greenhouses.SingleAsync(e => e.Id == 50);
            house.Name = "Свежая теплица"; house.X = 600; house.IsLocked = true;
            await db.SaveChangesAsync();
        });

        var result = (await SaveAsync(greenhouse, locked)).ValueOrThrow();

        Assert.Equal((50, locked), (result.Id, result.IsLocked));
        var storedBed = await _fixture.QueryAsync(db => db.GardenElements.OfType<Bed>().Include(e => e.PlantingSpots).SingleAsync(e => e.Id == 50));
        var storedHouse = await _fixture.QueryAsync(db => db.Greenhouses.Include(h => h.Elements).Include(h => h.Greenhouses).SingleAsync(h => h.Id == 50));
        Assert.Equal(greenhouse || locked, storedBed.IsLocked);
        Assert.Equal(!greenhouse || locked, storedHouse.IsLocked);
        Assert.Equal(("Свежая грядка", 60.0, 45.0, 150.0, 90.0, 30.0, 0.6),
            (storedBed.Name, storedBed.X, storedBed.Y, storedBed.DisplayWidth, storedBed.DisplayHeight, storedBed.Rotation, storedBed.AreaSquareMeters));
        Assert.Equal(("Заметка", "Почва", "Север", 2, 3), (storedBed.Note, storedBed.SoilType, storedBed.Orientation, storedBed.GridRows, storedBed.GridColumns));
        Assert.Equal("Ячейка", Assert.Single(storedBed.PlantingSpots).Note);
        Assert.Equal(("Свежая теплица", 600.0, 45.0, 30.0, 6.0, 3.0, 900.0, 450.0, 200.0, 150.0),
            (storedHouse.Name, storedHouse.X, storedHouse.Y, storedHouse.Rotation, storedHouse.WidthMeters, storedHouse.HeightMeters,
                storedHouse.CanvasWidth, storedHouse.CanvasHeight, storedHouse.DisplayWidth, storedHouse.DisplayHeight));
        Assert.Equal(("Заметка", "Стекло"), (storedHouse.Note, storedHouse.Material));
        Assert.Single(storedHouse.Elements); Assert.Single(storedHouse.Greenhouses);
        Assert.Equal("История", await _fixture.QueryAsync(db => db.SeedlingInfos.Select(s => s.Note).SingleAsync()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Set_MissingTarget_ReturnsFailure(bool greenhouse)
    {
        await PrepareAsync();
        Assert.True((await SaveAsync(greenhouse, true, 999)).IsFailure);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Set_DatabaseRejectsUpdate_PreservesStoredState(bool greenhouse)
    {
        await PrepareAsync();
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(greenhouse
            ? "CREATE TRIGGER RejectLock BEFORE UPDATE ON GardenPlots BEGIN SELECT RAISE(ABORT, 'lock rejected'); END;"
            : "CREATE TRIGGER RejectLock BEFORE UPDATE ON GardenElements BEGIN SELECT RAISE(ABORT, 'lock rejected'); END;"));
        var result = await SaveAsync(greenhouse, true);
        Assert.Contains("lock rejected", result.Match(_ => "", error => error));
        Assert.False(await _fixture.QueryAsync(db => db.GardenElements.Where(e => e.Id == 50).Select(e => e.IsLocked).SingleAsync()));
        Assert.False(await _fixture.QueryAsync(db => db.Greenhouses.Where(e => e.Id == 50).Select(e => e.IsLocked).SingleAsync()));
    }
}
