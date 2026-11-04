using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;

namespace Models.Tests;

public class HarvestSeasonSummaryTests
{
    [Fact]
    public void Season_PropagatesInvalidBatchInsteadOfDroppingIt()
    {
        var invalid = new HarvestPlanningInput { PerPlantKg = Option.Some(-1m) };
        Assert.True(HarvestSeasonSummary.Calculate([(new(), 2), (invalid, 2)]).IsFailure);
    }

    [Fact]
    public void Season_ReturnsFailureOnTotalOverflowAndCountsUnknownForecasts()
    {
        var huge = new HarvestPlanningInput { PerPlantKg = Option.Some(decimal.MaxValue) };
        Assert.True(HarvestSeasonSummary.Calculate([(huge, 1), (huge, 1)]).IsFailure);
        var known = new HarvestPlanningInput { PerPlantKg = Option.Some(2m), PerM2Kg = Option.Some(4m), YieldAreaM2 = Option.Some(1.8m) };
        var summary = HarvestSeasonSummary.Calculate([(known, 3), (new(), 2)]).ValueOrThrow();
        Assert.Equal(6m, summary.PerPlantKg); Assert.Equal(7.2m, summary.PerM2Kg);
        Assert.Equal(1, summary.UnknownPerPlantCount); Assert.Equal(1, summary.UnknownPerM2Count);
    }
}
