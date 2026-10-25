using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using BonfireDB.Planting;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class GardenDeletionWritingTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    private Task<Result<GardenDeletionResultFromViewModel>> DeleteAsync(int id)
        => new DeleteGarden(new DbGardenDeletionWriter(_fixture.Factory)).ExecuteAsync(new(id));

    private Task<int> PrepareAsync(bool greenhouse) => _fixture.QueryAsync(async db =>
    {
        var history = new SeedlingInfo { Note = "История", Replants = [new() { ReplantingNote = "Пересадка" }],
            Treatments = [new() { Product = "Обработка" }] };
        var plan = new GardenPlan { Name = "План", Year = 2026 };
        var garden = new Garden { Name = "Удалить", GardenPlan = plan,
            Elements = [new Bed { Name = "Дочерний", PlantingSpots = [new() { SeedlingInfo = history }] }] };
        if (greenhouse) garden.Greenhouses.Add(new() { Name = "Теплица" });
        db.Gardens.Add(garden);
        db.Gardens.Add(new() { Name = "Оставить", GardenPlan = plan,
            Elements = [new Bed { Name = "Чужой", PlantingSpots = [new() { SeedlingInfo = history }] }] });
        await db.SaveChangesAsync();
        return garden.Id;
    });

    [Fact]
    public async Task Delete_WithoutGreenhouses_PreservesPlanOtherGardenAndHistory()
    {
        var id = await PrepareAsync(false);
        Assert.True((await _fixture.CreateGardenService().DeleteGardenByIdAsync(new(id))).IsSuccess);
        Assert.Equal("Оставить", await _fixture.QueryAsync(db => db.Gardens.Select(g => g.Name).SingleAsync()));
        Assert.Equal("Чужой", await _fixture.QueryAsync(db => db.GardenElements.Select(e => e.Name).SingleAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.GardenPlans.CountAsync()));
        Assert.Equal("История", await _fixture.QueryAsync(db => db.SeedlingInfos.Select(s => s.Note).SingleAsync()));
        Assert.Equal("Пересадка", await _fixture.QueryAsync(db => db.Replants.Select(r => r.ReplantingNote).SingleAsync()));
        Assert.Equal("Обработка", await _fixture.QueryAsync(db => db.Treatments.Select(t => t.Product).SingleAsync()));
    }

    [Fact]
    public async Task Delete_WithGreenhouse_RestrictPreservesWholeTree()
    {
        var id = await PrepareAsync(true);
        Assert.True((await _fixture.CreateGardenService().DeleteGardenByIdAsync(new(id))).IsFailure);
        Assert.Equal(2, await _fixture.QueryAsync(db => db.Gardens.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Greenhouses.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.GardenElements.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
    }
    [Fact]
    public async Task Delete_CheckedRequest_RemovesOnlyTarget()
    {
        var id = await PrepareAsync(false);
        Assert.Equal(id, (await DeleteAsync(id)).ValueOrThrow().GardenId);
        Assert.Equal("Оставить", await _fixture.QueryAsync(db => db.Gardens.Select(g => g.Name).SingleAsync()));
        Assert.Equal("Чужой", await _fixture.QueryAsync(db => db.GardenElements.Select(e => e.Name).SingleAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Replants.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Treatments.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.GardenPlans.CountAsync()));
    }

    [Fact]
    public async Task Delete_CheckedRequestWithGreenhouse_ReturnsReasonWithoutChangingTree()
    {
        var id = await PrepareAsync(true);
        Assert.Contains("теплицами", (await DeleteAsync(id)).Match(_ => "", error => error));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.Gardens.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Greenhouses.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
    }

    [Fact]
    public async Task Delete_DatabaseRejectsChild_RollsBackWholeCascade()
    {
        var id = await PrepareAsync(false);
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectGardenChild BEFORE DELETE ON PlantingSpots BEGIN SELECT RAISE(ABORT, 'garden rejected'); END;"));
        Assert.Contains("garden rejected", (await DeleteAsync(id)).Match(_ => "", error => error));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.Gardens.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.GardenElements.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_MissingOrWrongTphTarget_ReturnsFailure(bool wrongType)
    {
        await PrepareAsync(true);
        var id = wrongType ? await _fixture.QueryAsync(db => db.Greenhouses.Select(h => h.Id).SingleAsync()) : 999;
        Assert.Contains("уже удалён", (await DeleteAsync(id)).Match(_ => "", error => error));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.Gardens.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Greenhouses.CountAsync()));
    }

    [Fact]
    public async Task Delete_GreenhouseAddedAfterUiRead_RefusesFreshTree()
    {
        var id = await PrepareAsync(false);
        await _fixture.LoadAsync<BonfireDB.Entities.GardenPlanning.Garden>(id);
        await _fixture.ExecuteAsync(async db => { db.Greenhouses.Add(new() { ParentPlotId = id, Name = "Новая" }); await db.SaveChangesAsync(); });
        Assert.Contains("теплицами", (await DeleteAsync(id)).Match(_ => "", error => error));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.Gardens.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
    }
}

