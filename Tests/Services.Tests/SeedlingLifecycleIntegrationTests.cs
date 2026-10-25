using Bonfire.Application.Seedlings;
using Bonfire.Domain.Seedlings;
using Bonfire.Domain.Functional;
using BonfireDB.Sowing;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class SeedlingLifecycleIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    private Task<int> PrepareAsync(bool sprout = true, double quantity = 4) => _fixture.QueryAsync(async db =>
    {
        var batch = new Seedling { Quantity = quantity, Plant = new() { PlantCulture = new() { Name = "Томат" },
            PlantSort = new() { Name = "Черри", Producer = new() { Name = "Фирма" } } },
            SeedlingInfos = [new() { SeedlingNumber = 0, Note = "Метаданные" }] };
        if (sprout) batch.SeedlingInfos.Add(new() { SeedlingNumber = 1, GerminationDate = new(2026, 3, 1) });
        db.Seedlings.Add(batch);
        await db.SaveChangesAsync();
        return batch.Id;
    });

    private Task<Result<SeedlingChangeResultFromViewModel>> ApplyAsync(int id, SeedlingAction action)
        => new ChangeSeedling(new DbSeedlingChangeWriter(_fixture.Factory)).ExecuteAsync(new(id, action));

    [Fact]
    public async Task Germinate_CapsAtFreshQuantityAndPreservesMetadata()
    {
        var id = await PrepareAsync();
        var date = new DateTime(2026, 3, 7);
        var result = await ApplyAsync(id, new SeedlingAction.Germinate(20, date));
        Assert.Equal(5, result.ValueOrThrow().Entries.Count);
        Assert.Equal(4, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync(info => info.GerminationDate != null)));
        Assert.Equal(3, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync(info => info.GerminationDate == date)));
        Assert.Equal("Метаданные", await _fixture.QueryAsync(db => db.SeedlingInfos.Where(info => info.SeedlingNumber == 0).Select(info => info.Note).SingleAsync()));
        Assert.Equal(new[] { 1, 2, 3, 4 }, await _fixture.QueryAsync(db => db.SeedlingInfos.Where(info => info.GerminationDate != null).OrderBy(info => info.SeedlingNumber).Select(info => info.SeedlingNumber).ToArrayAsync()));
        Assert.Equal(0, await _fixture.QueryAsync(db => db.Seeds.CountAsync()));
    }

    [Fact]
    public async Task Germinate_SecondInsertRejected_RollsBackAllNewSprouts()
    {
        var id = await PrepareAsync(false);
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectSprout BEFORE INSERT ON SeedlingInfos WHEN NEW.SeedlingNumber = 2 BEGIN SELECT RAISE(ABORT, 'sprout rejected'); END;"));
        var result = await ApplyAsync(id, new SeedlingAction.Germinate(3, DateTime.Today));
        Assert.Contains("sprout rejected", result.Match(_ => "не ожидалось", error => error));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.SeedlingInfos.CountAsync()));
        Assert.Equal(1, await _fixture.QueryAsync(db => db.Seedlings.CountAsync()));
    }

    [Fact]
    public async Task Germinate_Zero_PreservesExistingMarkDeadBehavior()
    {
        var id = await PrepareAsync(false);
        Assert.True((await ApplyAsync(id, new SeedlingAction.Germinate(0, DateTime.Today))).IsSuccess);
        Assert.True(await _fixture.QueryAsync(db => db.SeedlingInfos.Select(info => info.IsDead == true).SingleAsync()));
    }

    [Fact]
    public async Task Germinate_MissingBatch_ReturnsFailure() => Assert.True(
        (await ApplyAsync(999, new SeedlingAction.Germinate(1, DateTime.Today))).IsFailure);
}
