using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;

namespace Models.Tests;

public class SeedlingAvailabilityTests
{
    [Theory]
    [InlineData(3, 1, 1, 1)]
    [InlineData(3, 0, 1, 2)]
    [InlineData(1, 0, 2, -1)]
    [InlineData(0, 0, 0, 0)]
    public void Availability_AgreesWithExistingReadModelAndKeepsHistoricalDeficit(int germinated, int dead, int planted, int available)
    {
        var entries = Enumerable.Range(1, germinated).Select(number => new SeedlingEntry(Option.Some(number), number,
            Option.Some(DateTime.Today), Option.Some(number <= dead))).ToArray();
        var batch = SeedlingBatch.Create(10, entries, planted).ValueOrThrow();
        var readModel = new SeedlingFromViewModel { PlantedOut = planted,
            SeedlingInfos = new(entries.Select(entry => new SeedlingInfoFromViewModel { GerminationData = DateTime.Today, IsDead = entry.IsDead.ToNullable() })) };
        Assert.Equal(germinated, batch.GerminatedCount);
        Assert.Equal(germinated - dead, batch.AliveCount);
        Assert.Equal(available, batch.AvailableCount);
        Assert.Equal(readModel.Balance, batch.AvailableCount);
    }

    [Fact]
    public void Transitions_PreservePlantedCountAndRecomputeAvailability()
    {
        var batch = SeedlingBatch.Create(10,
            [new(Option.Some(1), 0, Option.None<DateTime>(), Option.None<bool>()),
             new(Option.Some(2), 1, Option.Some(DateTime.Today), Option.None<bool>())], 1).ValueOrThrow();
        var germinated = batch.WithGermination(2, DateTime.Today).ValueOrThrow();
        var replanted = germinated.WithReplanting(DateTime.Today).ValueOrThrow();
        var changed = replanted.WithDeadSprouts(1, Option.Some("Причина")).ValueOrThrow();
        Assert.Equal(1, germinated.PlantedOut);
        Assert.Equal(1, replanted.PlantedOut);
        Assert.Equal(1, changed.PlantedOut);
        Assert.Equal(1, changed.AvailableCount);
        Assert.Equal(0, batch.AvailableCount);
        var details = SeedlingDetails.Create(new() { Quantity = 9, LandingDate = Option.Some(DateTime.Today), Source = Option.Some("Из семян") }).ValueOrThrow();
        var edited = SeedlingEdit.Create(7, details, []).ValueOrThrow().ApplyTo(changed).ValueOrThrow();
        Assert.Equal(1, edited.PlantedOut);
        Assert.Equal(1, edited.AvailableCount);
    }

    [Fact]
    public void Create_NegativePlantedCount_ReturnsFailure() => Assert.True(SeedlingBatch.Create(10, [], -1).IsFailure);
}
