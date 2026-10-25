using BonfireDB.Entities;
using BonfireDB.SeedEditing;
using Bonfire.Domain.Functional;
using Microsoft.EntityFrameworkCore;

namespace Services.Tests;

public class SeedReadingTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task Read_SavedSeed_PreservesFieldsAndAbsenceWithoutEfReferences()
    {
        await _fixture.ExecuteAsync(async db =>
        {
            db.Seeds.Add(new Seed { Plant = new() { PlantCulture = new() { Name = "Культура", Class = "Мой класс" },
                PlantSort = new() { Name = "Сорт", Producer = new() { Name = "Фирма" }, Description = "", PlantHeight = 0 } },
                SeedsInfo = new() { AmountSeeds = 2.5, AmountSeedsWeight = null, WeightPack = 0.8,
                    QuantityPack = 10, CostPack = 100, Note = " ", DisposeComment = "", SeedSource = null } });
            await db.SaveChangesAsync();
        });
        var seed = Assert.Single((await new DbSeedReader(_fixture.Factory).ReadAsync(default)).ValueOrThrow());
        Assert.Equal("Культура", seed.Plant.PlantCulture.Name); Assert.Equal("Мой класс", seed.Plant.PlantCulture.Class);
        Assert.Equal("Сорт", seed.Plant.PlantSort.Name); Assert.Equal("Фирма", seed.Plant.PlantSort.Producer.Name);
        Assert.Equal("", seed.Plant.PlantSort.Characteristics.Description.ToNullable());
        Assert.Equal(0, seed.Plant.PlantSort.Characteristics.PlantHeight.ToNullable());
        Assert.Equal(2.5, seed.SeedsInfo.AmountSeeds); Assert.Equal(0.8, seed.SeedsInfo.WeightPack); Assert.Equal(10, seed.SeedsInfo.QuantityPack);
        Assert.Equal(100, seed.SeedsInfo.CostPack); Assert.IsType<Option<double>.None>(seed.SeedsInfo.AmountSeedsWeight);
        Assert.Equal(" ", seed.SeedsInfo.Note.ToNullable()); Assert.Equal("", seed.SeedsInfo.DisposeComment.ToNullable());
        Assert.IsType<Option<string>.None>(seed.SeedsInfo.SeedSource);
        await _fixture.ExecuteAsync(async db => { var row = await db.SeedsInfo.SingleAsync(); row.AmountSeeds = 1; await db.SaveChangesAsync(); });
        Assert.Equal(2.5, seed.SeedsInfo.AmountSeeds);
    }

    [Fact]
    public async Task Read_InvalidPackage_ReturnsIdAndDoesNotNormalize()
    {
        var id = await _fixture.QueryAsync(async db =>
        {
            var seed = new Seed { Plant = new() { PlantCulture = new() { Name = "Культура" }, PlantSort = new() { Name = "Сорт", Producer = new() { Name = "Фирма" } } },
                SeedsInfo = new() { AmountSeeds = -1 } };
            db.Seeds.Add(seed); await db.SaveChangesAsync(); return seed.Id;
        });
        var error = (await new DbSeedReader(_fixture.Factory).ReadAsync(default)).Match(_ => "", e => e);
        Assert.Contains($"Семена {id}", error);
        Assert.Equal(-1, await _fixture.QueryAsync(db => db.SeedsInfo.Select(s => s.AmountSeeds).SingleAsync()));
    }
}
