using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using BonfireDB.Planting;
using BonfireDB.Entities.GardenPlanning;
using Bonfire.Domain.Garden.States;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class GreenhouseDeletionWritingTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    private Task<Result<GreenhouseDeletionResultFromViewModel>> DeleteAsync(int id)
        => new DeleteGreenhouse(new DbGreenhouseDeletionWriter(_fixture.Factory)).ExecuteAsync(new(id));

    private Task<int> PrepareAsync(bool nested, bool inner = false) => _fixture.QueryAsync(async db =>
    {
        var garden = new Garden { Name = "Участок", GardenPlan = new() { Name = "План", Year = 2026 } };
        var shared = new SeedlingInfo { Note = "Общая история" };
        var house = new Greenhouse { Id = 50, Name = "Удалить", IsLocked = true, StateTypeName = nameof(ActiveState),
            ParentPlot = inner ? new Greenhouse { Name = "Внешняя", ParentPlot = garden } : garden,
            Elements = [new Bed { Name = "Внутри", PlantingSpots = [new() { Note = "Первая", SeedlingInfo = shared },
                new() { Note = "Вторая", SeedlingInfo = new() { Note = "Самостоятельная история",
                    Replants = [new() { ReplantingDate = new(2026, 5, 1), ReplantingNote = "Пересадка" }],
                    Treatments = [new() { TreatmentDate = new(2026, 6, 1), Product = "Обработка" }] } }] }] };
        if (nested) house.Greenhouses.Add(new() { Name = "Вложенная", Elements = [new Bed { Name = "Вложенный элемент" }] });
        db.Greenhouses.Add(house);
        db.GardenElements.Add(new Bed { Name = "Оставить", Plot = garden,
            PlantingSpots = [new() { Note = "Чужая ячейка", SeedlingInfo = shared }] });
        await db.SaveChangesAsync();
        return house.Id;
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_LeafGreenhouse_RemovesChildrenButPreservesHistoryAndParent(bool inner)
    {
        var id = await PrepareAsync(false, inner);
        Assert.Equal(id, (await DeleteAsync(id)).ValueOrThrow().GreenhouseId);

        Assert.False(await _fixture.QueryAsync(db => db.Greenhouses.AnyAsync(h => h.Id == id)));
        Assert.Equal(inner ? 1 : 0, await _fixture.QueryAsync(db => db.Greenhouses.CountAsync()));
        Assert.Equal("Оставить", await _fixture.QueryAsync(db => db.GardenElements.Select(e => e.Name).SingleAsync()));
        var spot = await _fixture.QueryAsync(db => db.PlantingSpots.Include(s => s.SeedlingInfo).SingleAsync());
        Assert.Equal("Чужая ячейка", spot.Note);
        Assert.Equal("Общая история", spot.SeedlingInfo!.Note);
        Assert.Equal(2, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
        Assert.Equal("Пересадка", await _fixture.QueryAsync(db => db.Replants.Select(r => r.ReplantingNote).SingleAsync()));
        Assert.Equal("Обработка", await _fixture.QueryAsync(db => db.Treatments.Select(t => t.Product).SingleAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Gardens.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.GardenPlans.CountAsync()));
    }

    [Fact]
    public async Task Delete_NestedGreenhouse_RestrictPreservesWholeTree()
    {
        var id = await PrepareAsync(true);
        Assert.Contains("вложенными теплицами", (await DeleteAsync(id)).Match(_ => "", error => error));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.Greenhouses.CountAsync()));
        Assert.Equal(3, await _fixture.QueryAsync(db => db.GardenElements.CountAsync()));
        Assert.Equal(3, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_DatabaseRejectsCascade_PreservesWholeTree(bool rejectHouse)
    {
        var id = await PrepareAsync(false);
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(rejectHouse
            ? "CREATE TRIGGER RejectHouse BEFORE DELETE ON GardenPlots BEGIN SELECT RAISE(ABORT, 'house rejected'); END;"
            : "CREATE TRIGGER RejectChild BEFORE DELETE ON PlantingSpots BEGIN SELECT RAISE(ABORT, 'house rejected'); END;"));
        var result = await DeleteAsync(id);
        Assert.Contains("house rejected", result.Match(_ => "", error => error));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Greenhouses.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.GardenElements.CountAsync()));
        Assert.Equal(3, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Replants.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Treatments.CountAsync()));
    }

    [Fact]
    public async Task Delete_MissingHouse_ReturnsFailureWithoutDeletingOthers()
    {
        await PrepareAsync(false);
        Assert.True((await DeleteAsync(999)).IsFailure);
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Greenhouses.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.GardenElements.CountAsync()));
    }

    [Fact]
    public async Task Delete_GardenId_ReturnsFailureWithoutDeletingRootContainer()
    {
        await PrepareAsync(false);
        var id = await _fixture.QueryAsync(db => db.Gardens.Select(g => g.Id).SingleAsync());
        Assert.True((await DeleteAsync(id)).IsFailure);
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Gardens.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Greenhouses.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.GardenElements.CountAsync()));
    }

    [Fact]
    public async Task Delete_ChildAddedAfterUiRead_RemovesFreshChildButPreservesHistory()
    {
        var id = await PrepareAsync(false);
        await _fixture.LoadAsync<BonfireDB.Entities.GardenPlanning.Greenhouse>(id);
        await _fixture.ExecuteAsync(async db =>
        {
            db.GardenElements.Add(new Bed { PlotId = id, Name = "Новый элемент", PlantingSpots = [new() { SeedlingInfo = new() { Note = "Новая история" } }] });
            await db.SaveChangesAsync();
        });
        (await DeleteAsync(id)).ValueOrThrow();
        Assert.Equal("Оставить", await _fixture.QueryAsync(db => db.GardenElements.Select(e => e.Name).SingleAsync()));
        Assert.True(await _fixture.QueryAsync(db => db.SeedlingInfos.AnyAsync(s => s.Note == "Новая история")));
    }

    [Fact]
    public async Task Delete_NestedHouseAddedAfterUiRead_RefusesWholeDeletion()
    {
        var id = await PrepareAsync(false);
        await _fixture.LoadAsync<BonfireDB.Entities.GardenPlanning.Greenhouse>(id);
        await _fixture.ExecuteAsync(async db => { db.Greenhouses.Add(new() { ParentPlotId = id, Name = "Новая вложенная" }); await db.SaveChangesAsync(); });
        Assert.Contains("вложенными теплицами", (await DeleteAsync(id)).Match(_ => "", error => error));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.Greenhouses.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.GardenElements.CountAsync()));
        Assert.Equal(3, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
    }

}
