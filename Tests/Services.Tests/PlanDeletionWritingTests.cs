using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using BonfireDB.Planting;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class PlanDeletionWritingTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    private Task<Result<GardenPlanDeletionResultFromViewModel>> DeleteAsync(int id)
        => new DeleteGardenPlan(new DbGardenPlanDeletionWriter(_fixture.Factory)).ExecuteAsync(new(id));

    private Task<int> PrepareAsync(bool greenhouse) => _fixture.QueryAsync(async db =>
    {
        var history = new SeedlingInfo { Note = "История", Replants = [new() { ReplantingNote = "Пересадка" }],
            Treatments = [new() { Product = "Обработка" }] };
        var plan = new GardenPlan { Name = "Удалить", Year = 2026, Gardens = [new() { Name = "Участок",
            Elements = [new Bed { PlantingSpots = [new() { SeedlingInfo = history }] }] }, new() { Name = "Второй" }] };
        if (greenhouse) plan.Gardens[0].Greenhouses.Add(new() { Name = "Теплица" });
        db.GardenPlans.Add(plan);
        db.GardenPlans.Add(new() { Name = "Оставить", Gardens = [new() { Name = "Чужой",
            Elements = [new Bed { PlantingSpots = [new() { SeedlingInfo = history }] }] }] });
        await db.SaveChangesAsync();
        return plan.Id;
    });

    [Fact]
    public async Task Delete_WithoutGreenhouses_RemovesGardensButPreservesHistoryAndOtherPlan()
    {
        var id = await PrepareAsync(false);
        await _fixture.CreateGardenService().DeletePlanAsync(new GardenPlan { Id = id });
        Assert.Equal("Оставить", await _fixture.QueryAsync(db => db.GardenPlans.Select(p => p.Name).SingleAsync()));
        Assert.Equal("Чужой", await _fixture.QueryAsync(db => db.Gardens.Select(p => p.Name).SingleAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.GardenElements.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
        Assert.Equal("История", await _fixture.QueryAsync(db => db.SeedlingInfos.Select(s => s.Note).SingleAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Replants.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Treatments.CountAsync()));
    }

    [Fact]
    public async Task Delete_WithGreenhouse_RestrictRollsBackAllGardens()
    {
        var id = await PrepareAsync(true);
        await Assert.ThrowsAsync<DbUpdateException>(() => _fixture.CreateGardenService().DeletePlanAsync(new GardenPlan { Id = id }));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.GardenPlans.CountAsync()));
        Assert.Equal(3, await _fixture.QueryAsync(db => db.Gardens.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Greenhouses.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
    }
    [Fact]
    public async Task Delete_CheckedRequest_PreservesOtherPlanAndHistory()
    {
        var id = await PrepareAsync(false);
        Assert.Equal(id, (await DeleteAsync(id)).ValueOrThrow().GardenPlanId);
        Assert.Equal("Оставить", await _fixture.QueryAsync(db => db.GardenPlans.Select(p => p.Name).SingleAsync()));
        Assert.Equal("Чужой", await _fixture.QueryAsync(db => db.Gardens.Select(p => p.Name).SingleAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Replants.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Treatments.CountAsync()));
    }

    [Fact]
    public async Task Delete_GreenhouseAddedAfterUiRead_RefusesEntirePlan()
    {
        var id = await PrepareAsync(false);
        await _fixture.CreateGardenService().GetPlansOrderedByYearDescAsync();
        await _fixture.ExecuteAsync(async db => { db.Greenhouses.Add(new() { ParentPlotId = await db.Gardens.Where(g => g.GardenPlanId == id).Select(g => g.Id).FirstAsync() }); await db.SaveChangesAsync(); });
        Assert.Contains("теплицами", (await DeleteAsync(id)).Match(_ => "", error => error));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.GardenPlans.CountAsync()));
        Assert.Equal(3, await _fixture.QueryAsync(db => db.Gardens.CountAsync()));
    }

    [Fact]
    public async Task Delete_DatabaseRejectsCascade_RollsBackAllGardens()
    {
        var id = await PrepareAsync(false);
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectPlanChild BEFORE DELETE ON PlantingSpots BEGIN SELECT RAISE(ABORT, 'plan rejected'); END;"));
        Assert.Contains("plan rejected", (await DeleteAsync(id)).Match(_ => "", error => error));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.GardenPlans.CountAsync()));
        Assert.Equal(3, await _fixture.QueryAsync(db => db.Gardens.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
    }

    [Fact]
    public async Task Delete_MissingPlan_ReturnsFailure()
    {
        await PrepareAsync(false);
        Assert.Contains("уже удалён", (await DeleteAsync(999)).Match(_ => "", error => error));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.GardenPlans.CountAsync()));
    }
}

