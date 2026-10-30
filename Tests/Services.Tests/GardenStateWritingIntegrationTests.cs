using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Domain.Garden.States;
using Bonfire.Domain.Garden.SpotStates;
using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Planting;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class GardenStateWritingIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    private Task<(int Bed, int Greenhouse, int Spot)> PrepareAsync() => _fixture.QueryAsync(async db =>
    {
        var plan = new GardenPlan { Name = "План", Year = 2026 };
        db.GardenPlans.Add(plan);
        await db.SaveChangesAsync();
        var garden = new Garden { GardenPlanId = plan.Id, Name = "Участок" };
        db.Gardens.Add(garden);
        await db.SaveChangesAsync();
        var greenhouse = new Greenhouse { ParentPlotId = garden.Id, Name = "Теплица", Note = "Сохранить" };
        db.Greenhouses.Add(greenhouse);
        await db.SaveChangesAsync();
        var spot = new PlantingSpot
        {
            Note = "История", StateTypeName = nameof(PlantedSpotState), PlantedDate = new(2026, 5, 1), HarvestDate = new(2026, 8, 1),
            SeedlingInfo = new() { Note = "Рассада", Replants = [new() { ReplantingDate = new(2026, 4, 1) }] }
        };
        var bed = new Bed { PlotId = greenhouse.Id, Name = "Грядка", Note = "Сохранить", IsLocked = true, PlantingSpots = [spot] };
        db.GardenElements.Add(bed);
        await db.SaveChangesAsync();
        return (bed.Id, greenhouse.Id, spot.Id);
    });

    private Task<Result<GardenStateResultFromViewModel>> ChangeAsync(int id, StateChangeTarget target)
        => new ChangeGardenState(new DbGardenStateWriter(_fixture.Factory)).ExecuteAsync(new(id, target));

    [Fact]
    public async Task Change_ElementAndGreenhouse_OnlyUpdatesStateAndPreservesComposite()
    {
        var ids = await PrepareAsync();
        Assert.IsType<GardenStateResultFromViewModel.Element>((await ChangeAsync(ids.Bed, new StateChangeTarget.Element(new PreparedState()))).ValueOrThrow());
        Assert.IsType<GardenStateResultFromViewModel.Greenhouse>((await ChangeAsync(ids.Greenhouse, new StateChangeTarget.Greenhouse(new PreparedState()))).ValueOrThrow());
        var bed = await _fixture.QueryAsync(db => db.GardenElements.SingleAsync());
        Assert.Equal(nameof(PreparedState), bed.StateTypeName);
        Assert.Equal(ids.Greenhouse, bed.PlotId);
        Assert.Equal("Сохранить", bed.Note);
        Assert.True(bed.IsLocked);
        Assert.Equal(nameof(PreparedState), await _fixture.QueryAsync(db => db.Greenhouses.Select(g => g.StateTypeName).SingleAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
    }

    [Fact]
    public async Task Change_FreshStateDisallowsTransition_ReturnsFailureWithoutOverwritingMetadata()
    {
        var ids = await PrepareAsync();
        await _fixture.ExecuteAsync(async db =>
        {
            var bed = await db.GardenElements.SingleAsync();
            bed.StateTypeName = nameof(ActiveState);
            bed.Note = "Новые данные";
            await db.SaveChangesAsync();
        });
        var result = await ChangeAsync(ids.Bed, new StateChangeTarget.Element(new PreparedState()));
        Assert.Contains("запрещён", result.Match(_ => "", error => error));
        Assert.Equal("Новые данные", await _fixture.QueryAsync(db => db.GardenElements.Select(b => b.Note).SingleAsync()));
        Assert.Equal(nameof(ActiveState), await _fixture.QueryAsync(db => db.GardenElements.Select(b => b.StateTypeName).SingleAsync()));
    }

    [Fact]
    public async Task Change_HarvestThenClear_PreservesHistoryAndClearsLinkAndDates()
    {
        var ids = await PrepareAsync();
        var harvested = Assert.IsType<GardenStateResultFromViewModel.Spot>((await ChangeAsync(ids.Spot,
            new StateChangeTarget.Spot(new SpotChange.Transition(new HarvestedSpotState(), Option.None<string>())))).ValueOrThrow()).Cell;
        Assert.Equal(nameof(HarvestedSpotState), harvested.StateTypeName);
        Assert.Equal(new DateTime(2026, 8, 1), harvested.HarvestDate.ToNullable());
        var cleared = Assert.IsType<GardenStateResultFromViewModel.Spot>((await ChangeAsync(ids.Spot, new StateChangeTarget.Spot(new SpotChange.Clear()))).ValueOrThrow()).Cell;
        Assert.Equal(nameof(EmptySpotState), cleared.StateTypeName);
        Assert.IsType<Option<int>.None>(cleared.SeedlingInfoId);
        Assert.IsType<Option<DateTime>.None>(cleared.PlantedDate);
        Assert.IsType<Option<DateTime>.None>(cleared.HarvestDate);
        Assert.Equal("История", cleared.Note.ToNullable());
        Assert.Equal(1, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Replants.CountAsync()));
    }

    [Fact]
    public async Task Change_SaveRejected_RollsBackStateAndLabel()
    {
        var ids = await PrepareAsync();
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectState BEFORE UPDATE ON PlantingSpots BEGIN SELECT RAISE(ABORT, 'state rejected'); END;"));
        var result = await ChangeAsync(ids.Spot, new StateChangeTarget.Spot(new SpotChange.Transition(new DeadSpotState(), Option.Some("Другое"))));
        Assert.Contains("state rejected", result.Match(_ => "", error => error));
        var spot = await _fixture.QueryAsync(db => db.PlantingSpots.SingleAsync());
        Assert.Equal(nameof(PlantedSpotState), spot.StateTypeName);
        Assert.Equal("История", spot.Note);
        Assert.NotNull(spot.SeedlingInfoId);
    }

    [Fact]
    public async Task Change_MissingObject_ReturnsFailure()
        => Assert.Contains("удалён", (await ChangeAsync(999, new StateChangeTarget.Element(new PreparedState()))).Match(_ => "", error => error));
}
