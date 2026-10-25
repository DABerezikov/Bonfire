using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Domain.Garden.States;
using Bonfire.Domain.Garden.SpotStates;
using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Planting;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class GridResizingIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    private Task<int> PrepareAsync() => _fixture.QueryAsync(async db =>
    {
        var plan = new GardenPlan { Name = "План", Year = 2026 };
        db.GardenPlans.Add(plan);
        await db.SaveChangesAsync();
        var garden = new Garden { GardenPlanId = plan.Id, Name = "Участок" };
        db.Gardens.Add(garden);
        await db.SaveChangesAsync();
        var bed = new Bed
        {
            PlotId = garden.Id, Name = "Грядка", GridRows = 1, GridColumns = 2, Note = "Метаданные", IsLocked = true,
            PlantingSpots = [
                new() { Row = 0, Column = 0, Note = "Оставить", StateTypeName = nameof(PlantedSpotState), PlantedDate = new(2026, 5, 1),
                    SeedlingInfo = new() { Note = "История 1" } },
                new() { Row = 0, Column = 1, Note = "Убрать", StateTypeName = nameof(HarvestedSpotState), HarvestDate = new(2026, 8, 1),
                    SeedlingInfo = new() { Note = "История 2", Replants = [new() { ReplantingDate = new(2026, 4, 1) }] } }
            ]
        };
        db.GardenElements.Add(bed);
        await db.SaveChangesAsync();
        return bed.Id;
    });

    private Task<Result<GridResultFromViewModel>> ResizeAsync(int id, int rows = 2, int columns = 1)
        => new ResizeGrid(new DbGridWriter(_fixture.Factory)).ExecuteAsync(new(id, rows, columns));

    [Fact]
    public async Task Resize_PreservesIdsMetadataAndIndependentHistory()
    {
        var id = await PrepareAsync();
        var originalId = await _fixture.QueryAsync(db => db.PlantingSpots.Where(s => s.Column == 0).Select(s => s.Id).SingleAsync());
        var result = (await ResizeAsync(id)).ValueOrThrow();
        Assert.Equal(2, result.Spots.Count);
        var kept = result.Spots.Single(s => s.Row == 0);
        Assert.Equal(originalId, kept.Id);
        Assert.Equal("Оставить", kept.Note.ToNullable());
        Assert.Equal(nameof(PlantedSpotState), kept.StateTypeName);
        Assert.Equal(new DateTime(2026, 5, 1), kept.PlantedDate.ToNullable());
        Assert.True(kept.SeedlingInfoId is Option<int>.Some);
        Assert.Equal(nameof(EmptySpotState), result.Spots.Single(s => s.Row == 1).StateTypeName);
        Assert.Equal(2, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Replants.CountAsync()));
        var bed = await _fixture.QueryAsync(db => db.GardenElements.SingleAsync());
        Assert.Equal("Метаданные", bed.Note);
        Assert.True(bed.IsLocked);
        Assert.Equal((2, 1), (bed.GridRows, bed.GridColumns));
    }

    [Fact]
    public async Task Resize_SameSize_PreservesHarvestDateAndAllCellIds()
    {
        var id = await PrepareAsync();
        var originalIds = await _fixture.QueryAsync(db => db.PlantingSpots.OrderBy(s => s.Column).Select(s => s.Id).ToArrayAsync());
        var receipt = (await ResizeAsync(id, 1, 2)).ValueOrThrow();
        Assert.Equal(originalIds, receipt.Spots.Select(s => s.Id));
        Assert.Equal(new DateTime(2026, 8, 1), receipt.Spots[1].HarvestDate.ToNullable());
        Assert.Equal("Убрать", receipt.Spots[1].Note.ToNullable());
        Assert.Equal(nameof(HarvestedSpotState), receipt.Spots[1].StateTypeName);
    }

    [Fact]
    public async Task Resize_ZeroDimension_ClearsGridButKeepsSeedlingHistory()
    {
        var id = await PrepareAsync();
        var receipt = (await ResizeAsync(id, 0, 2)).ValueOrThrow();
        Assert.Empty(receipt.Spots);
        Assert.Equal(0, receipt.Rows);
        Assert.Equal(0, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Replants.CountAsync()));
    }

    [Fact]
    public async Task Resize_FreshStateBlocked_DoesNotChangeGrid()
    {
        var id = await PrepareAsync();
        await _fixture.ExecuteAsync(async db =>
        {
            var bed = await db.GardenElements.SingleAsync();
            bed.StateTypeName = nameof(ActiveState);
            await db.SaveChangesAsync();
        });
        var result = await ResizeAsync(id);
        Assert.Contains("запрещено", result.Match(_ => "", error => error));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.PlantingSpots.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.GardenElements.Select(b => b.GridRows).SingleAsync()));
    }

    [Fact]
    public async Task Resize_SaveRejected_RollsBackRemovalAdditionAndDimensions()
    {
        var id = await PrepareAsync();
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectGrid BEFORE INSERT ON PlantingSpots BEGIN SELECT RAISE(ABORT, 'grid rejected'); END;"));
        var result = await ResizeAsync(id);
        Assert.Contains("grid rejected", result.Match(_ => "", error => error));
        Assert.Equal(new[] { "Оставить", "Убрать" }, await _fixture.QueryAsync(db => db.PlantingSpots.OrderBy(s => s.Column).Select(s => s.Note).ToArrayAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.GardenElements.Select(b => b.GridRows).SingleAsync()));
        Assert.Equal(2, await _fixture.QueryAsync(db => db.GardenElements.Select(b => b.GridColumns).SingleAsync()));
    }

    [Fact]
    public async Task Resize_MissingElement_ReturnsExplicitFailure()
        => Assert.Contains("удалён", (await ResizeAsync(999)).Match(_ => "", error => error));
}
