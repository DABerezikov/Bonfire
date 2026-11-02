using BonfireDB.Entities;
using BonfireDB.Planning;

namespace Services.Tests;

public class PlanningStockChoicesTests : IDisposable
{
    private readonly SqliteUowFixture _fixture = new();
    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task Choices_DistinguishLibraryHistoryFromPositivePiecesOrWeightAndCombinePackages()
    {
        await _fixture.ExecuteAsync(async db =>
        {
            Plant Plant(string name) => new() { PlantCulture = new() { Name = name, Class = "Овощи" },
                PlantSort = new() { Name = "Сорт", Producer = new() { Name = "Фирма" } } };
            var history = Plant("Арбуз"); var empty = Plant("Пустой пакет");
            var pieces = Plant("Штуки"); var grams = Plant("Граммы");
            db.Plants.Add(history);
            db.Seeds.AddRange(new Seed { Plant = empty, SeedsInfo = new() { AmountSeeds = 0, AmountSeedsWeight = 0 } },
                new Seed { Plant = pieces, SeedsInfo = new() { AmountSeeds = 10 } },
                new Seed { Plant = grams, SeedsInfo = new() { AmountSeedsWeight = .2 } },
                new Seed { Plant = new Plant { PlantCulture = pieces.PlantCulture, PlantSort = pieces.PlantSort }, SeedsInfo = new() { AmountSeeds = 0 } });
            await db.SaveChangesAsync();
        });
        var choices = (await new DbPlanningContextReader(_fixture.Factory).ReadChoicesAsync(default)).ValueOrThrow();
        Assert.Equal(4, choices.Length);
        Assert.False(choices.Single(x => x.CultureName == "Арбуз").HasAvailableSeeds);
        Assert.False(choices.Single(x => x.CultureName == "Пустой пакет").HasAvailableSeeds);
        Assert.True(choices.Single(x => x.CultureName == "Штуки").HasAvailableSeeds);
        Assert.True(choices.Single(x => x.CultureName == "Граммы").HasAvailableSeeds);
    }
}
