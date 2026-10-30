using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Planting;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class GardenCreationWritingTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();
    private CreateGarden CreateGarden() => new(new DbGardenCreationWriter(_fixture.Factory));

    [Fact]
    public async Task CreatePlan_LocalYear_PersistsDefaultNameAndAbsentDescription()
    {
        var clock = Substitute.For<TimeProvider>(); clock.GetUtcNow().Returns(new DateTimeOffset(2025, 12, 31, 22, 0, 0, TimeSpan.Zero));
        clock.LocalTimeZone.Returns(TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(3), "Test", "Test"));
        var saved = (await new CreateGardenPlan(new DbGardenPlanCreationWriter(_fixture.Factory), clock).ExecuteAsync()).ValueOrThrow();
        Assert.Equal(2026, saved.Year); Assert.Equal("План 2026", saved.Name);
        Assert.IsType<Option<string>.None>(saved.Description);
        var row = await _fixture.QueryAsync(db => db.GardenPlans.SingleAsync());
        Assert.Equal(saved.Id, row.Id); Assert.Equal(saved.Name, row.Name); Assert.Null(row.Description);
    }

    [Theory]
    [InlineData(20, 15, "  Участок  ")]
    [InlineData(0, 0, "")]
    public async Task CreateGarden_ValidDimensions_PreservesTextAndCreatesEmptyCanvas(double width, double height, string name)
    {
        var plan = await _fixture.PreparePlanAsync("План", 2026);
        var saved = (await CreateGarden().ExecuteAsync(new(plan.Id, name, width, height, 150))).ValueOrThrow();
        Assert.Equal(name, saved.Values.Name); Assert.Equal(width * 150, saved.Canvas.Width); Assert.Equal(height * 150, saved.Canvas.Height);
        var row = await _fixture.QueryAsync(db => db.Gardens.SingleAsync());
        Assert.Equal(plan.Id, row.GardenPlanId); Assert.Equal(name, row.Name);
        Assert.Equal(saved.Canvas.Width, row.CanvasWidth); Assert.Null(row.Address); Assert.Null(row.Note);
        Assert.Equal(0, await _fixture.QueryAsync(db => db.GardenElements.CountAsync()));
    }

    [Fact]
    public async Task CreateGarden_DeletedPlan_RefusesWithoutOrphan()
    {
        var plan = await _fixture.PreparePlanAsync("План", 2026);
        await _fixture.CreateGardenService().DeletePlanByIdAsync(new(plan.Id));
        Assert.Contains("план", (await CreateGarden().ExecuteAsync(new(plan.Id, "Участок", 20, 15, 150))).Match(_ => "", e => e));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Gardens.CountAsync()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_DatabaseRejectsWrite_ReturnsFailureWithoutPartialRows(bool plan)
    {
        var parent = await _fixture.PreparePlanAsync("Прежний", 2026);
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(plan
            ? "CREATE TRIGGER RejectPlan BEFORE INSERT ON GardenPlans BEGIN SELECT RAISE(ABORT, 'create rejected'); END;"
            : "CREATE TRIGGER RejectGarden BEFORE INSERT ON GardenPlots BEGIN SELECT RAISE(ABORT, 'create rejected'); END;"));
        var error = plan
            ? (await new CreateGardenPlan(new DbGardenPlanCreationWriter(_fixture.Factory), TimeProvider.System).ExecuteAsync()).Match(_ => "", e => e)
            : (await CreateGarden().ExecuteAsync(new(parent.Id, "Участок", 20, 15, 150))).Match(_ => "", e => e);
        Assert.Contains("create rejected", error);
        Assert.Equal(1, await _fixture.QueryAsync(db => db.GardenPlans.CountAsync()));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Gardens.CountAsync()));
    }

    [Theory]
    [InlineData(0, 1, 1, 150)]
    [InlineData(1, -1, 1, 150)]
    [InlineData(1, double.NaN, 1, 150)]
    [InlineData(1, 1, double.PositiveInfinity, 150)]
    [InlineData(1, 1, 1, 0)]
    [InlineData(1, double.MaxValue, 1, 150)]
    public async Task CreateGarden_InvalidValues_DoNotReachWriter(int planId, double width, double height, double scale)
    {
        var writer = Substitute.For<IGardenCreationWriter>();
        Assert.True((await new CreateGarden(writer).ExecuteAsync(new(planId, "Имя", width, height, scale))).IsFailure);
        await writer.DidNotReceive().CreateAsync(Arg.Any<GardenCreation>(), Arg.Any<CancellationToken>());
    }
}
