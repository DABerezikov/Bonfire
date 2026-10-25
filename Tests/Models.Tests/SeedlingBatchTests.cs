using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;

namespace Models.Tests;

public class SeedlingBatchTests
{
    private static SeedlingEntry Entry(int id, bool germinated, bool? dead = null) => new(
        Option.Some(id), id - 1, germinated ? Option.Some(new DateTime(2026, 3, 1)) : Option.None<DateTime>(), dead.ToOption());

    [Theory]
    [InlineData(3, 20, 2)]
    [InlineData(0, 4, 4)]
    [InlineData(2.5, 20, 1)]
    [InlineData(1, 20, 0)]
    public void WithGermination_LimitAndExistingSprouts_ProducesImmutableChange(double quantity, int requested, int added)
    {
        var original = SeedlingBatch.Create(quantity, [Entry(1, false), Entry(2, true, true)]).ValueOrThrow();
        var date = new DateTime(2026, 3, 7);
        var updated = original.WithGermination(requested, date).ValueOrThrow();
        Assert.Equal(1 + added, updated.GerminatedCount);
        Assert.Equal(added, updated.AliveCount);
        Assert.Equal(2, original.Entries.Length);
        Assert.All(updated.Entries.Skip(2), entry =>
        {
            Assert.True(entry.Id is Option<int>.None);
            Assert.Equal(date, entry.GerminationDate.ToNullable());
        });
        Assert.Equal(Enumerable.Range(2, added), updated.Entries.Skip(2).Select(entry => entry.Number));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void WithGermination_Zero_MarksOnlySingleEntryDead(int extraEntries)
    {
        var entries = new List<SeedlingEntry> { Entry(1, false) };
        if (extraEntries > 0) entries.Add(Entry(2, true));
        var original = SeedlingBatch.Create(3, entries).ValueOrThrow();
        var updated = original.WithGermination(0, DateTime.Today).ValueOrThrow();
        Assert.Equal(extraEntries == 0, updated.Entries[0].IsDead.ToNullable() == true);
        Assert.Null(original.Entries[0].IsDead.ToNullable());
    }

    [Fact]
    public void WithGermination_NegativeCount_ReturnsFailure() => Assert.True(
        SeedlingBatch.Create(3, [Entry(1, false)]).ValueOrThrow().WithGermination(-1, DateTime.Today).IsFailure);

    [Fact]
    public void WithGermination_NoMetadataAndZero_ReturnsExplicitFailure() => Assert.True(
        SeedlingBatch.Create(3, []).ValueOrThrow().WithGermination(0, DateTime.Today).IsFailure);

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Create_InvalidQuantity_ReturnsFailure(double quantity) => Assert.True(SeedlingBatch.Create(quantity, []).IsFailure);

    [Fact]
    public void Create_CopiesMutableInput()
    {
        var entries = new List<SeedlingEntry> { Entry(1, false) };
        var batch = SeedlingBatch.Create(3, entries).ValueOrThrow();
        entries.Clear();
        Assert.Single(batch.Entries);
    }

    [Fact]
    public void WithReplanting_RecordsOnlyFirstEntryWithoutMutatingOriginal()
    {
        var original = SeedlingBatch.Create(3, [Entry(1, false), Entry(2, true)]).ValueOrThrow();
        var date = new DateTime(2026, 4, 1);
        var updated = original.WithReplanting(date).ValueOrThrow();
        Assert.Equal(date, Assert.Single(updated.Entries[0].Replants).Date);
        Assert.Empty(updated.Entries[1].Replants);
        Assert.Empty(original.Entries[0].Replants);
        Assert.True(updated.WithReplanting(date.AddDays(1)).IsFailure);
    }

    [Fact]
    public void WithReplanting_MissingMetadata_ReturnsFailure() => Assert.True(
        SeedlingBatch.Create(3, []).ValueOrThrow().WithReplanting(DateTime.Today).IsFailure);
}
