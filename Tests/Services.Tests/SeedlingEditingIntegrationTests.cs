using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;
using BonfireDB.Sowing;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class SeedlingEditingIntegrationTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    private Task<(int BatchId, int InfoId)> PrepareAsync() => _fixture.QueryAsync(async db =>
    {
        var batch = new Seedling { Quantity = 3, Weight = 0.5, SeedId = 999, PlantedOut = 1, IsPlantedInBed = true,
            LandingDate = new(2026, 3, 1), Plant = new() { PlantCulture = new() { Name = "Томат" },
                PlantSort = new() { Name = "Черри", Producer = new() { Name = "Фирма" } } },
            SeedlingInfos = [new() { Note = "Исходная", QuarantineCause = "Карантин", Replants = [new() { ReplantingNote = "История" }] }] };
        db.Seedlings.Add(batch);
        await db.SaveChangesAsync();
        return (batch.Id, batch.SeedlingInfos[0].Id);
    });

    private Task<Result<SeedlingEditResultFromViewModel>> SaveAsync(int id, int infoId)
    {
        var values = new SeedlingDetailsValues { Quantity = 7, Weight = 1.5, LandingDate = Option.Some(new DateTime(2026, 4, 1)),
            Source = Option.Some("Подарено"), Place = Option.Some("Окно"), Note = Option.Some(" Новая "), LunarPhase = "Фаза" };
        var edit = SeedlingEdit.Create(id, SeedlingDetails.Create(values).ValueOrThrow(),
            [new(infoId, 2, Option.Some(new DateTime(2026, 4, 5)), Option.Some("Причина"))]).ValueOrThrow();
        return new DbSeedlingEditWriter(_fixture.Factory).SaveAsync(edit, CancellationToken.None);
    }

    [Fact]
    public async Task Edit_UpdatesOnlyEditableFieldsAndSynchronizesMetadata()
    {
        var (id, infoId) = await PrepareAsync();
        Assert.True((await SaveAsync(id, infoId)).IsSuccess);
        var saved = await _fixture.LoadAsync<Seedling>(id);
        Assert.Equal(7, saved!.Quantity);
        Assert.Equal(1.5, saved.Weight);
        Assert.Equal(saved.LandingDate, saved.SeedlingInfos[0].LandingDate);
        Assert.Equal(new DateTime(2026, 4, 1), saved.LandingDate);
        Assert.Equal("Подарено", saved.SeedlingSource);
        Assert.Equal("Фаза", saved.LunarPhase);
        Assert.Equal(" Новая ", saved.SeedlingInfos[0].Note);
        Assert.Equal("Карантин", saved.SeedlingInfos[0].QuarantineCause);
        Assert.Equal("История", Assert.Single(saved.SeedlingInfos[0].Replants!).ReplantingNote);
        Assert.Equal(999, saved.SeedId);
        Assert.Equal(1, saved.PlantedOut);
        Assert.True(saved.IsPlantedInBed);
        Assert.Equal("Черри", saved.Plant.PlantSort.Name);
    }

    [Fact]
    public async Task Edit_SaveRejected_RollsBackBatchAndRecord()
    {
        var (id, infoId) = await PrepareAsync();
        await _fixture.ExecuteAsync(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER RejectEdit BEFORE UPDATE ON SeedlingInfos BEGIN SELECT RAISE(ABORT, 'edit rejected'); END;"));
        var result = await SaveAsync(id, infoId);
        Assert.Contains("edit rejected", result.Match(_ => "не ожидалось", error => error));
        Assert.Equal(3, await _fixture.QueryAsync(db => db.Seedlings.Select(batch => batch.Quantity).SingleAsync()));
        Assert.Equal("Исходная", await _fixture.QueryAsync(db => db.SeedlingInfos.Select(info => info.Note).SingleAsync()));
    }

    [Fact]
    public async Task Edit_WrongRowId_DoesNotChangeBatch()
    {
        var (id, _) = await PrepareAsync();
        Assert.True((await SaveAsync(id, 9999)).IsFailure);
        Assert.Equal(3, await _fixture.QueryAsync(db => db.Seedlings.Select(batch => batch.Quantity).SingleAsync()));
    }
}
