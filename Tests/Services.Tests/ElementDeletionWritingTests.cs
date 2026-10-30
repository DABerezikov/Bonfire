using BonfireDB.Entities.GardenPlanning;
using Bonfire.Application.Garden;
using BonfireDB.Planting;
using Bonfire.Domain.Garden.States;
using Bonfire.Domain.Garden.SpotStates;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class ElementDeletionWritingTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    private Task<Bonfire.Domain.Functional.Result<ElementDeletionResultFromViewModel>> DeleteAsync(int id)
        => new DeleteGardenElement(new DbElementDeletionWriter(_fixture.Factory)).ExecuteAsync(new(id));

    private Task<int> PrepareAsync(string kind, bool inner) => _fixture.QueryAsync(async db =>
    {
        var garden = new Garden { Name = "Участок", GardenPlan = new() { Name = "План", Year = 2026 } };
        var house = new Greenhouse { Name = "Теплица", ParentPlot = garden };
        db.Greenhouses.Add(house);
        GardenElement element = kind switch
        {
            "Bed" => new Bed(), "ColdFrame" => new ColdFrame(), "FlowerBed" => new FlowerBed(), _ => new OpenGroundArea()
        };
        var shared = new SeedlingInfo { Note = "Общая история" };
        element.Plot = inner ? house : garden;
        element.Name = "Удалить"; element.IsLocked = true; element.StateTypeName = nameof(ActiveState);
        element.PlantingSpots = [new() { Note = "Первая", StateTypeName = nameof(PlantedSpotState), SeedlingInfo = shared },
            new() { Note = "Вторая", StateTypeName = nameof(HarvestedSpotState), SeedlingInfo = new() { Note = "Самостоятельная история",
                Replants = [new() { ReplantingNote = "Пересадка", ReplantingDate = new(2026, 5, 1) }],
                Treatments = [new() { Product = "Обработка", TreatmentDate = new(2026, 6, 1) }] } }];
        db.GardenElements.Add(element);
        db.GardenElements.Add(new Bed { Name = "Оставить", Plot = garden,
            PlantingSpots = [new() { Note = "Чужая ячейка", SeedlingInfo = shared }] });
        await db.SaveChangesAsync();
        return element.Id;
    });

    [Theory]
    [InlineData("Bed", false)]
    [InlineData("ColdFrame", false)]
    [InlineData("FlowerBed", false)]
    [InlineData("OpenGroundArea", false)]
    [InlineData("Bed", true)]
    [InlineData("ColdFrame", true)]
    [InlineData("FlowerBed", true)]
    [InlineData("OpenGroundArea", true)]
    public async Task Delete_RemovesOnlyElementAndItsCells_PreservesHistoryAndContainers(string kind, bool inner)
    {
        var id = await PrepareAsync(kind, inner);
        Assert.Equal(id, (await DeleteAsync(id)).ValueOrThrow().ElementId);

        Assert.Equal("Оставить", await _fixture.QueryAsync(db => db.GardenElements.Select(e => e.Name).SingleAsync()));
        var spot = await _fixture.QueryAsync(db => db.PlantingSpots.Include(s => s.SeedlingInfo).SingleAsync());
        Assert.Equal("Чужая ячейка", spot.Note);
        Assert.Equal("Общая история", spot.SeedlingInfo!.Note);
        Assert.Equal(2, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
        Assert.Equal("Пересадка", await _fixture.QueryAsync(db => db.Replants.Select(r => r.ReplantingNote).SingleAsync()));
        Assert.Equal("Обработка", await _fixture.QueryAsync(db => db.Treatments.Select(t => t.Product).SingleAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Gardens.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Greenhouses.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.GardenPlans.CountAsync()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_DatabaseRejectsDeletion_RollsBackElementAndCells(bool rejectCell)
    {
        var id = await PrepareAsync("Bed", false);
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(rejectCell
            ? "CREATE TRIGGER RejectDeletion BEFORE DELETE ON PlantingSpots BEGIN SELECT RAISE(ABORT, 'deletion rejected'); END;"
            : "CREATE TRIGGER RejectDeletion BEFORE DELETE ON GardenElements BEGIN SELECT RAISE(ABORT, 'deletion rejected'); END;"));
        var result = await DeleteAsync(id);
        Assert.Contains("deletion rejected", result.Match(_ => "", error => error));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.GardenElements.CountAsync()));
        Assert.Equal(3, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Replants.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Treatments.CountAsync()));
    }

    [Fact]
    public async Task Delete_MissingElement_ReturnsFailureWithoutDeletingOtherData()
    {
        await PrepareAsync("Bed", false);
        Assert.True((await DeleteAsync(999)).IsFailure);
        Assert.Equal(2, await _fixture.QueryAsync(db => db.GardenElements.CountAsync()));
        Assert.Equal(3, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
    }

    [Fact]
    public async Task Delete_CellAddedAfterUiRead_DeletesFreshCellsButPreservesTheirHistory()
    {
        var id = await PrepareAsync("Bed", false);
        await _fixture.LoadAsync<BonfireDB.Entities.GardenPlanning.GardenElement>(id);
        await _fixture.ExecuteAsync(async db =>
        {
            db.PlantingSpots.Add(new() { GardenElementId = id, Note = "Новая ячейка", SeedlingInfo = new() { Note = "Новая история" } });
            await db.SaveChangesAsync();
        });
        (await DeleteAsync(id)).ValueOrThrow();
        Assert.Equal(0, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync(s => s.GardenElementId == id)));
        Assert.True(await _fixture.QueryAsync(db => db.SeedlingInfos.AnyAsync(s => s.Note == "Новая история")));
    }
}
