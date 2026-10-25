using Bonfire.Domain.Seeds;

namespace Models.Tests;

public class SpotSeedSowingTests
{
    [Theory]
    [InlineData(10, 3, SeedUnit.Pieces, 3, 7)]
    [InlineData(2, 5, SeedUnit.Pieces, 2, 0)]
    [InlineData(10, 2.5, SeedUnit.Pieces, 2, 8)]
    [InlineData(10, 1.5, SeedUnit.Pieces, 2, 8)]
    [InlineData(1.2, 3, SeedUnit.Grams, 1.2, 0)]
    [InlineData(1.2, 0.2, SeedUnit.Grams, 0.2, 1)]
    public void SowUpTo_ClampsThenRoundsPiecesAndPreservesStock(double available, double requested, SeedUnit unit, double sown, double remaining)
    {
        var original = new SeedStock(SeedAmount.Create(available, unit).ValueOrThrow());
        var result = original.SowUpTo(SeedAmount.Create(requested, unit).ValueOrThrow()).ValueOrThrow();
        Assert.Equal(sown, result.Sown.Value);
        Assert.Equal(remaining, result.Remaining.Available.Value, 10);
        Assert.Equal(unit, result.Sown.Unit);
        Assert.Equal(available, original.Available.Value);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(3, 0.5)]
    [InlineData(0.6, 1)]
    public void SowUpTo_EmptyOrImpossibleRoundedConsumption_ReturnsFailure(double available, double requested)
        => Assert.True(new SeedStock(SeedAmount.Create(available, SeedUnit.Pieces).ValueOrThrow())
            .SowUpTo(SeedAmount.Create(requested, SeedUnit.Pieces).ValueOrThrow()).IsFailure);

    [Fact]
    public void SowUpTo_DifferentUnits_ReturnsFailure()
        => Assert.True(new SeedStock(SeedAmount.Create(10, SeedUnit.Pieces).ValueOrThrow())
            .SowUpTo(SeedAmount.Create(1, SeedUnit.Grams).ValueOrThrow()).IsFailure);
}
