using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;
using Bonfire.Domain.Seeds;

namespace Models.Tests;

public class SeedlingRemovalTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidSeedlingId_ReturnsFailure(int id) => Assert.True(SeedlingId.Create(id).IsFailure);

    [Fact]
    public void Create_PositiveSeedlingId_PreservesValue() => Assert.Equal(7, SeedlingId.Create(7).ValueOrThrow().Value);

    [Fact]
    public void ReturnTo_BothUnits_ReturnsNewPackageWithoutMutatingOriginal()
    {
        var original = SeedPackage.Create(new() { AmountSeeds = 7, AmountSeedsWeight = Option.Some(1.0) }).ValueOrThrow();
        var result = new SeedlingRemoval.CorrectSowing().ReturnTo(original, 3, 0.5).ValueOrThrow();
        Assert.Equal(10, result.Values.AmountSeeds);
        Assert.Equal(1.5, result.Values.AmountSeedsWeight.Match(value => value, () => -1));
        Assert.Equal(7, original.Values.AmountSeeds);
        Assert.Equal(1, original.Values.AmountSeedsWeight.Match(value => value, () => -1));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.PositiveInfinity)]
    public void ReturnTo_InvalidBatchAmounts_ReturnsFailure(double quantity, double weight)
    {
        var original = SeedPackage.Create(new() { AmountSeeds = 7 }).ValueOrThrow();
        Assert.True(new SeedlingRemoval.CorrectSowing().ReturnTo(original, quantity, weight).IsFailure);
        Assert.Equal(7, original.Values.AmountSeeds);
    }

    [Fact]
    public void ReturnTo_SecondUnitFails_DoesNotPartiallyChangeOriginal()
    {
        var original = SeedPackage.Create(new() { AmountSeeds = 7 }).ValueOrThrow();
        Assert.True(new SeedlingRemoval.CorrectSowing().ReturnTo(original, 3, 0.5).IsFailure);
        Assert.Equal(7, original.Values.AmountSeeds);
        Assert.True(original.Values.AmountSeedsWeight.Match(_ => false, () => true));
    }
}
