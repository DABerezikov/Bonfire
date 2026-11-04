using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;

namespace Models.Tests;

public class SeedQuantityCalculationTests
{
    [Theory]
    [InlineData(20, 80, 10, 28)]
    [InlineData(1, 100, 0, 1)]
    [InlineData(20, 80, 0, 25)]
    [InlineData(100, 99.9, 0, 101)]
    public void Automatic_RoundsUpOnceAfterLosses(int target, decimal germination, decimal losses, int expected)
    {
        var result = SeedQuantityCalculation.Automatic(PlantCount.Create(target).ValueOrThrow(),
            GerminationPercentage.Create(germination).ValueOrThrow(), LossPercentage.Create(losses).ValueOrThrow()).ValueOrThrow();
        Assert.Equal(expected, result.Seeds.Value);
        Assert.False(result.IsManual);
    }

    [Fact]
    public void Automatic_OverflowOrDecimalUnderflow_ReturnsFailure()
    {
        var count = PlantCount.Create(int.MaxValue).ValueOrThrow();
        Assert.True(SeedQuantityCalculation.Automatic(count, GerminationPercentage.Create(50).ValueOrThrow(),
            LossPercentage.Create(0).ValueOrThrow()).IsFailure);
        Assert.True(SeedQuantityCalculation.Automatic(count, GerminationPercentage.Create(0.0000000000000000000000000001m).ValueOrThrow(),
            LossPercentage.Create(0).ValueOrThrow()).IsFailure);
    }

    [Fact]
    public void Manual_KeepsSeedCountAndShowsDeficit_UnknownRatesStayUnknown()
    {
        var target = PlantCount.Create(20).ValueOrThrow();
        var seeds = PlantCount.Create(10).ValueOrThrow();
        var known = SeedQuantityCalculation.Manual(target, seeds,
            Option.Some(GerminationPercentage.Create(80).ValueOrThrow()), Option.Some(LossPercentage.Create(10).ValueOrThrow()));
        Assert.Equal(10, known.Seeds.Value);
        Assert.Equal(Option.Some(7.2m), known.ExpectedPlants);
        Assert.True(known.HasDeficit);
        var unknown = SeedQuantityCalculation.Manual(target, seeds, Option.None<GerminationPercentage>(), Option.None<LossPercentage>());
        Assert.Equal(10, unknown.Seeds.Value);
        Assert.IsType<Option<decimal>.None>(unknown.ExpectedPlants);
        Assert.True(unknown.IsManual);
    }
}
