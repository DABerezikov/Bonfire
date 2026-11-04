using Bonfire.Domain.Functional;
using Bonfire.Domain.Harvest;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;

namespace Models.Tests;

public class HarvestJournalTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Mass_MustBePositive(string text) => Assert.True(HarvestMass.Create(decimal.Parse(text)).IsFailure);

    [Fact]
    public void Entry_RejectsFutureAndKeepsEmptyNoteDistinctFromNone()
    {
        var today = new DateOnly(2026, 11, 1);
        Assert.True(HarvestEntry.Create(Identity(), today.AddDays(1), HarvestMass.Create(1m).ValueOrThrow(), Option.None<string>(), today).IsFailure);
        var entry = HarvestEntry.Create(Identity(), today, HarvestMass.Create(1.25m).ValueOrThrow(), Option.Some(""), today).ValueOrThrow();
        Assert.Equal(Option.Some(""), entry.Note);
    }

    [Fact]
    public void Summary_DistinctIdsAndCheckedMass()
    {
        var today = new DateOnly(2026, 11, 1);
        var entry = HarvestEntry.Create(Identity(), today, HarvestMass.Create(1.25m).ValueOrThrow(), Option.None<string>(), today).ValueOrThrow();
        var one = new RecordedHarvest(HarvestEntryId.Create(1).ValueOrThrow(), entry);
        var two = new RecordedHarvest(HarvestEntryId.Create(2).ValueOrThrow(), HarvestEntry.Create(Identity(), today.AddDays(-1), HarvestMass.Create(2m).ValueOrThrow(), Option.None<string>(), today).ValueOrThrow());
        var result = HarvestSummary.Calculate([one, one, two]).ValueOrThrow();
        Assert.Equal(3.25m, result.TotalKg); Assert.Equal(2, result.Count); Assert.Equal(Option.Some(today.AddDays(-1)), result.FirstDate);
        var huge = new RecordedHarvest(HarvestEntryId.Create(3).ValueOrThrow(), HarvestEntry.Create(Identity(), today, HarvestMass.Create(decimal.MaxValue).ValueOrThrow(), Option.None<string>(), today).ValueOrThrow());
        Assert.True(HarvestSummary.Calculate([one, huge]).IsFailure);
    }

    private static HarvestIdentity Identity() => HarvestIdentity.Create(CultureId.Create(1).ValueOrThrow(), SortId.Create(2).ValueOrThrow(),
        "Томат", "Черри", 2026, "Томск", "Asia/Tomsk", Option.None<PlannedBatchId>(), Option.None<PerennialPlantingId>()).ValueOrThrow();
}
