using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;

namespace Models.Tests;

public class SeedlingPlantingTests
{
    private static SeedlingBatch Batch(int planted = 1) => SeedlingBatch.Create(0,
        [new(Option.Some(1), 1, Option.Some(DateTime.Today), Option.None<bool>()),
         new(Option.Some(2), 2, Option.Some(DateTime.Today), Option.Some(false)),
         new(Option.Some(3), 3, Option.Some(DateTime.Today), Option.None<bool>()),
         new(Option.Some(4), 4, Option.Some(DateTime.Today), Option.Some(true)),
         new(Option.Some(5), 0, Option.None<DateTime>(), Option.None<bool>())], planted).ValueOrThrow();

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 2)]
    [InlineData(1.5, 2)]
    [InlineData(0.6, 1)]
    public void PlantOut_UsesLiveAvailableSproutsAndPreservesOriginal(double requested, int quantity)
    {
        var original = Batch();
        var change = original.PlantOut(requested).ValueOrThrow();
        Assert.Equal(quantity, change.Quantity);
        Assert.Equal(1 + quantity, change.Remaining.PlantedOut);
        Assert.Equal(2 - quantity, change.Remaining.AvailableCount);
        Assert.Equal(original.Quantity, change.Remaining.Quantity);
        Assert.Equal(original.Entries, change.Remaining.Entries);
        Assert.Equal(1, original.PlantedOut);
        Assert.Equal(2, original.AvailableCount);
    }

    [Theory]
    [InlineData(2.5, 2)]
    [InlineData(1.5, 2)]
    public void PlantOut_PreservesRoundingToEven(double requested, int quantity)
    {
        Assert.Equal(quantity, Batch(0).PlantOut(requested).ValueOrThrow().Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PlantOut_InvalidOrRoundedToZero_ReturnsFailure(double requested)
        => Assert.True(Batch().PlantOut(requested).IsFailure);

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void PlantOut_NoAvailableSprouts_ReturnsFailureWithoutHidingDeficit(int planted)
    {
        var batch = Batch(planted);
        Assert.True(batch.PlantOut(1).IsFailure);
        Assert.Equal(3 - planted, batch.AvailableCount);
    }
}
