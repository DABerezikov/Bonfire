using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Planning;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class PlanningBedReadTests
{
    [Fact]
    public async Task Read_IndependentFreshDimensionsDoesNotWriteGrid()
    {
        using var fixture = new SqliteUowFixture();
        var bedId = 0;
        await fixture.ExecuteAsync(async db =>
        {
            var bed = new Bed { Name = "Овощи", Plot = new Garden { Name = "Участок", GardenPlan = new() { Name = "План", Year = 2027 } }, DisplayWidth = 300, DisplayHeight = 135,
                GridRows = 2, GridColumns = 5 };
            db.Add(bed); await db.SaveChangesAsync(); bedId = bed.Id;
        });
        var reader = new DbPlanningBedReader(fixture.Factory);
        var first = (await reader.ReadAsync(default)).ValueOrThrow().Single();
        await fixture.ExecuteAsync(async db => { (await db.Set<GardenElement>().SingleAsync(x => x.Id == bedId)).DisplayWidth = 450; await db.SaveChangesAsync(); });
        var second = (await reader.ReadAsync(default)).ValueOrThrow().Single();
        Assert.Equal(300, first.WidthPixels); Assert.Equal(450, second.WidthPixels);
        Assert.Equal(2, await fixture.QueryAsync(db => db.Set<GardenElement>().Select(x => x.GridRows).SingleAsync()));
        Assert.Equal(5, await fixture.QueryAsync(db => db.Set<GardenElement>().Select(x => x.GridColumns).SingleAsync()));
    }
}
