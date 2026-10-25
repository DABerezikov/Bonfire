using Bonfire.Domain.Functional;
using BonfireDB.Entities;
using BonfireDB.Sowing;

namespace Services.Tests;

public class SeedlingReadingTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task Read_IndependentBatch_PreservesMetadataHistoryAndDeficit()
    {
        var id = await _fixture.QueryAsync(async db =>
        {
            var batch = new Seedling { Plant = new() { PlantCulture = new() { Name = "Культура" },
                PlantSort = new() { Name = "Сорт", Producer = new() { Name = "Фирма" } } },
                SeedId = 999, Quantity = 0, Weight = 0.5, PlantedOut = 3, LunarPhase = "", PlantPlace = " ",
                SeedlingInfos = [new() { Note = "Метаданные", LandingDate = new(2026, 3, 1),
                    QuarantineCause = "Причина", QuarantineStartDate = new(2026, 3, 2), QuenchingDate = new(2026, 3, 3),
                    Replants = [new() { ReplantingDate = new(2026, 3, 4), PotVolume = 0, ReplantingNote = "" }],
                    Treatments = [new() { TreatmentDate = new(2026, 3, 5), Product = "Препарат", TreatmentMethod = "" }] },
                    new() { SeedlingNumber = 1, GerminationDate = new(2026, 3, 6), IsDead = false }] };
            db.Seedlings.Add(batch); await db.SaveChangesAsync(); return batch.Id;
        });
        var reader = new DbSeedlingReader(_fixture.Factory);
        var batch = Assert.Single((await reader.ReadAsync(default)).ValueOrThrow());
        Assert.Equal(id, batch.Id); Assert.Equal(999, batch.SeedId); Assert.Equal(3, batch.PlantedOut);
        Assert.Equal(0, batch.Quantity); Assert.Equal(0.5, batch.Weight); Assert.Equal("", batch.LunarPhase.ToNullable());
        Assert.Equal(-2, new Bonfire.Models.SeedlingReadModel(batch).ToBatch().ValueOrThrow().AvailableCount);
        Assert.Equal(" ", batch.PlantPlace.ToNullable()); Assert.IsType<Option<DateTime>.None>(batch.LandingDate);
        var metadata = batch.SeedlingInfos[0]; Assert.Equal("Метаданные", metadata.Note.ToNullable());
        Assert.Equal("Причина", metadata.QuarantineCause.ToNullable()); Assert.NotNull(metadata.QuarantineStartDate.ToNullable());
        Assert.Equal(new DateTime(2026, 3, 3), metadata.QuenchingDate.ToNullable());
        var replant = Assert.Single(metadata.Entry.Replants); Assert.Equal(0, replant.PotVolume.ToNullable()); Assert.Equal("", replant.Note.ToNullable());
        var treatment = Assert.Single(metadata.Treatments.Match(v => v, () => throw new Exception()));
        Assert.Equal("Препарат", treatment.Product.ToNullable()); Assert.Equal("", treatment.TreatmentMethod.ToNullable());
        Assert.Equal(batch.Id, (await reader.ReadAsync(id, default)).ValueOrThrow().Match(v => v.Id, () => -1));
    }

    [Fact]
    public async Task Read_MissingBatch_ReturnsNone()
        => Assert.IsType<Option<Bonfire.Application.Seedlings.SeedlingReadFromViewModel>.None>(
            (await new DbSeedlingReader(_fixture.Factory).ReadAsync(999, default)).ValueOrThrow());
}
