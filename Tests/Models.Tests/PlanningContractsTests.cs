using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Libraries;
using Xunit;

namespace Models.Tests;

public class PlanningContractsTests
{
    [Fact]
    public void DateWindow_InclusiveSingleDayAndYearCrossing_PreservesBounds()
    {
        var day = new DateOnly(2027, 1, 1);
        Assert.True(DateWindow.Create(day, day).IsSuccess);
        Assert.True(DateWindow.Create(day.AddDays(-1), day).IsSuccess);
        Assert.True(DateWindow.Create(day, day.AddDays(-1)).IsFailure);
    }

    [Fact]
    public void DateWindow_ShiftOverflow_ReturnsFailureWithoutChangingSource()
    {
        var window = DateWindow.Create(DateOnly.MaxValue, DateOnly.MaxValue).ValueOrThrow();
        Assert.True(window.Shift(1, 1).IsFailure);
        Assert.Equal(DateOnly.MaxValue, window.Start);
        Assert.True(window.Shift(int.MinValue, int.MaxValue).IsFailure);
    }

    [Theory]
    [InlineData(-1, 2, false)]
    [InlineData(2, 1, false)]
    [InlineData(0, 0, true)]
    [InlineData(1, int.MaxValue, true)]
    public void DayRange_ValidatesNonNegativeOrderedBounds(int min, int max, bool valid)
        => Assert.Equal(valid, DayRange.Create(min, max).IsSuccess);

    [Theory]
    [InlineData(0, false, true)]
    [InlineData(100, true, false)]
    [InlineData(-1, false, false)]
    [InlineData(101, false, false)]
    [InlineData(99.9, true, true)]
    public void Percentages_UseDifferentLimitsForGerminationAndLosses(decimal value, bool germination, bool losses)
    {
        Assert.Equal(germination, GerminationPercentage.Create(value).IsSuccess);
        Assert.Equal(losses, LossPercentage.Create(value).IsSuccess);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    [InlineData(int.MaxValue, true)]
    public void IdsAndCounts_RequirePositiveIntegers(int value, bool valid)
    {
        Assert.Equal(valid, RegionId.Create(value).IsSuccess);
        Assert.Equal(valid, SeasonalPlantingPlanId.Create(value).IsSuccess);
        Assert.Equal(valid, PlannedBatchId.Create(value).IsSuccess);
        Assert.Equal(valid, PlantCount.Create(value).IsSuccess);
    }

    [Fact]
    public void GrowingConditions_WindowsillRequiresContainer()
    {
        Assert.True(GrowingConditions.Create(GrowingLocation.Windowsill, PlantPlacement.InGround).IsFailure);
        Assert.True(GrowingConditions.Create(GrowingLocation.Windowsill, PlantPlacement.Container).IsSuccess);
        Assert.True(GrowingConditions.Create(GrowingLocation.Greenhouse, PlantPlacement.Container).IsSuccess);
        Assert.True(GrowingConditions.Create((GrowingLocation)99, PlantPlacement.Container).IsFailure);
    }

    [Fact]
    public void ParameterResolution_UsesWholeRangesAndPreservesZero()
    {
        var culture = new RangeInput(Option.Some(5), Option.Some(10));
        var sort = new RangeInput(Option.Some(0), Option.Some(0));
        var resolved = PlanningParameters.ResolveRange("Прорастание", Option.None<DayRange>(), sort, culture).ValueOrThrow();
        Assert.Equal(ParameterSource.Sort, resolved.Source);
        Assert.Equal(0, resolved.Value.Min);
        Assert.Equal(0, resolved.Value.Max);
        Assert.True(PlanningParameters.ResolveRange("Прорастание", Option.None<DayRange>(),
            new RangeInput(Option.Some(3), Option.None<int>()), culture).IsFailure);
    }

    [Fact]
    public void ParameterResolution_BatchOverridesPartialSortAndMissingIsExplicit()
    {
        var none = new RangeInput(Option.None<int>(), Option.None<int>());
        var partial = new RangeInput(Option.Some(3), Option.None<int>());
        var batch = DayRange.Create(4, 8).ValueOrThrow();
        var result = PlanningParameters.ResolveRange("Возраст", Option.Some(batch), partial, none).ValueOrThrow();
        Assert.Equal(new SourcedValue<DayRange>(batch, ParameterSource.Batch), result);
        Assert.True(PlanningParameters.ResolveRange("Возраст", Option.None<DayRange>(), none, none).IsFailure);
        var fallback = PlanningParameters.ResolveRange("Возраст", Option.None<DayRange>(), none,
            new RangeInput(Option.Some(10), Option.Some(20))).ValueOrThrow();
        Assert.Equal(ParameterSource.Culture, fallback.Source);
    }

    [Fact]
    public void OperationOverride_DisabledDiffersFromInheritedAndEnabled()
    {
        var range = DayRange.Create(10, 15).ValueOrThrow();
        var inherited = Option.Some(new SourcedValue<DayRange>(range, ParameterSource.Culture));
        Assert.IsType<Option<SourcedValue<DayRange>>.None>(new OperationOverride<DayRange>.Disabled().Resolve(inherited));
        Assert.Equal(inherited, new OperationOverride<DayRange>.Inherit().Resolve(inherited));
        var enabled = new OperationOverride<DayRange>.Enabled(range).Resolve(Option.None<SourcedValue<DayRange>>());
        Assert.Equal(ParameterSource.Batch, enabled.Match(value => value.Source, () => throw new Exception()));
    }

    [Fact]
    public void GrowingMethod_DirectSowCarriesOnlySowingWindow()
    {
        var window = DateWindow.Create(new(2027, 5, 1), new(2027, 5, 10)).ValueOrThrow();
        GrowingMethod direct = new GrowingMethod.DirectSow(window);
        GrowingMethod transplant = new GrowingMethod.Transplant(window);
        Assert.NotEqual(direct, transplant);
        Assert.Equal(window, Assert.IsType<GrowingMethod.DirectSow>(direct).SowingWindow);
    }

    [Fact]
    public void Batch_IndependentReplacement_PreservesOtherBatchAndOriginalSnapshot()
    {
        var original = CreateBatch(1);
        var other = CreateBatch(2);
        var plan = SeasonalPlantingPlan.Create(SeasonalPlantingPlanId.Create(1).ValueOrThrow(),
            2027, RegionId.Create(1).ValueOrThrow(), [original, other]).ValueOrThrow();
        var changed = original.WithChoices(new PlanningChoices(Option.Some(new DateOnly(2027, 4, 1)),
            Option.None<DateOnly>(), Option.Some(PlantCount.Create(12).ValueOrThrow()))).ValueOrThrow();
        var updated = plan.WithBatch(changed).ValueOrThrow();
        Assert.Equal(Option.None<DateOnly>(), plan.Batches[0].Choices.SowingDate);
        Assert.Equal(new DateOnly(2027, 4, 1), updated.Batches[0].Choices.SowingDate.Match(x => x, () => default));
        Assert.Same(other, updated.Batches[1]);
        Assert.Equal(original.Profile, changed.Profile);
    }

    [Fact]
    public void Plan_RejectsDuplicateBatchesAndInvalidYear()
    {
        var batch = CreateBatch(1);
        var id = SeasonalPlantingPlanId.Create(1).ValueOrThrow();
        var region = RegionId.Create(1).ValueOrThrow();
        Assert.True(SeasonalPlantingPlan.Create(id, 2027, region, [batch, batch]).IsFailure);
        Assert.True(SeasonalPlantingPlan.Create(id, 0, region, []).IsFailure);
        Assert.True(SeasonalPlantingPlan.Create(id, 10000, region, []).IsFailure);
    }

    [Fact]
    public void Batch_DirectSowDoesNotRequireSeedlingAge_TransplantDoes()
    {
        var batch = CreateBatch(1);
        var window = ((GrowingMethod.DirectSow)batch.Method).SowingWindow;
        Assert.True(PlannedBatch.Create(batch.Id, batch.CultureId, batch.SortId, batch.Conditions,
            new GrowingMethod.Transplant(window), batch.Profile, batch.TargetPlants, batch.Choices).IsFailure);
    }

    private static PlannedBatch CreateBatch(int id)
    {
        var range = new SourcedValue<DayRange>(DayRange.Create(5, 10).ValueOrThrow(), ParameterSource.Culture);
        var profile = new EffectiveGrowingProfile(range, Option.None<SourcedValue<DayRange>>(),
            Option.None<SourcedValue<DayRange>>());
        return PlannedBatch.Create(PlannedBatchId.Create(id).ValueOrThrow(),
            CultureId.Create(1).ValueOrThrow(), SortId.Create(2).ValueOrThrow(),
            GrowingConditions.Create(GrowingLocation.OpenGround, PlantPlacement.InGround).ValueOrThrow(),
            new GrowingMethod.DirectSow(DateWindow.Create(new(2027, 5, 1), new(2027, 5, 10)).ValueOrThrow()),
            profile, PlantCount.Create(10).ValueOrThrow(), PlanningChoices.Empty).ValueOrThrow();
    }
}
