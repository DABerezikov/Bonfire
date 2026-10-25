using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;

namespace Models.Tests;

public class SeedlingEditTests
{
    public static SeedlingDetailsValues Values() => new() { Quantity = 5, Weight = 0.5,
        LandingDate = Option.Some(new DateTime(2026, 3, 1)), Source = Option.Some("Из семян"), Note = Option.Some(" Заметка ") };

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(1, -1)]
    [InlineData(double.NaN, 0)]
    [InlineData(1, double.PositiveInfinity)]
    public void Details_InvalidAmounts_ReturnsFailure(double quantity, double weight) => Assert.True(
        SeedlingDetails.Create(Values() with { Quantity = quantity, Weight = weight }).IsFailure);

    [Fact]
    public void Details_MissingRequiredValues_ReturnsFailure()
    {
        Assert.True(SeedlingDetails.Create(Values() with { LandingDate = Option.None<DateTime>() }).IsFailure);
        Assert.True(SeedlingDetails.Create(Values() with { Source = Option.None<string>() }).IsFailure);
    }

    [Fact]
    public void ApplyTo_UpdatesOnlyEditableFieldsAndPreservesNewerRecords()
    {
        var original = SeedlingBatch.Create(3, [new(Option.Some(1), 0, Option.None<DateTime>(), Option.None<bool>()),
            new(Option.Some(2), 1, Option.Some(DateTime.Today), Option.Some(true))]).ValueOrThrow();
        var edit = SeedlingEdit.Create(7, SeedlingDetails.Create(Values()).ValueOrThrow(),
            [new(1, 4, Option.Some(new DateTime(2026, 4, 1)), Option.Some(" Причина "))]).ValueOrThrow();
        var result = edit.ApplyTo(original).ValueOrThrow();
        Assert.Equal(5, result.Quantity);
        Assert.Equal(4, result.Entries[0].Number);
        Assert.Equal(" Причина ", result.Entries[0].DeathNote.ToNullable());
        Assert.Null(result.Entries[0].IsDead.ToNullable());
        Assert.Equal(original.Entries[1], result.Entries[1]);
        Assert.Equal(3, original.Quantity);
        Assert.Equal(0, original.Entries[0].Number);
    }

    [Fact]
    public void ApplyTo_MissingRow_ReturnsFailure()
    {
        var edit = SeedlingEdit.Create(7, SeedlingDetails.Create(Values()).ValueOrThrow(),
            [new(99, 1, Option.None<DateTime>(), Option.None<string>())]).ValueOrThrow();
        Assert.True(edit.ApplyTo(SeedlingBatch.Create(3, []).ValueOrThrow()).IsFailure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidRowId_ReturnsFailure(int id) => Assert.True(SeedlingEdit.Create(7,
        SeedlingDetails.Create(Values()).ValueOrThrow(), [new(id, 0, Option.None<DateTime>(), Option.None<string>())]).IsFailure);

    [Fact]
    public void Create_DuplicateRowId_ReturnsFailure()
    {
        var row = new SeedlingEntryEdit(1, 0, Option.None<DateTime>(), Option.None<string>());
        Assert.True(SeedlingEdit.Create(7, SeedlingDetails.Create(Values()).ValueOrThrow(), [row, row]).IsFailure);
    }
}
