using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;

namespace Models.Tests;

public class PlantingScheduleTests
{
    [Fact]
    public void Transplant_FixedDate_ReturnsPossibleResilientAndMeanWindows()
    {
        var target = Window(2027, 5, 20, 2027, 5, 20);
        var result = PlantingSchedule.Calculate(new GrowingMethod.Transplant(target), Profile(5, 10, 50, 60),
            PlanningChoices.Empty, Option.None<EffectivePlanningExtras>()).ValueOrThrow();
        Assert.Equal(Window(2027, 3, 11, 2027, 3, 26), result.Sowing);
        Assert.Equal(Option.Some(Window(2027, 3, 16, 2027, 3, 21)), result.ResilientSowing);
        Assert.Equal(Window(2027, 3, 18, 2027, 3, 18), result.MeanReference);
    }

    [Fact]
    public void Transplant_SeparateCeilings_MatchWorkbookExample()
    {
        var result = PlantingSchedule.Calculate(new GrowingMethod.Transplant(Window(2022, 5, 25, 2022, 5, 30)),
            Profile(10, 15, 65, 70), PlanningChoices.Empty, Option.None<EffectivePlanningExtras>()).ValueOrThrow();
        Assert.Equal(Window(2022, 3, 5, 2022, 3, 10), result.MeanReference);
    }

    [Fact]
    public void Transplant_WideGerminationUncertainty_HasNoResilientWindow()
    {
        var result = PlantingSchedule.Calculate(new GrowingMethod.Transplant(Window(2027, 5, 20, 2027, 5, 20)),
            Profile(1, 30, 50, 55), PlanningChoices.Empty, Option.None<EffectivePlanningExtras>()).ValueOrThrow();
        Assert.IsType<Option<DateWindow>.None>(result.ResilientSowing);
    }

    [Fact]
    public void DirectSow_UsesEmergenceOnceForMaturity_HasNoTransplantStages()
    {
        var profile = Profile(5, 10, 50, 60) with { Vegetation = Option.Some(Source(30, 40)) };
        var result = PlantingSchedule.Calculate(new GrowingMethod.DirectSow(Window(2027, 5, 1, 2027, 5, 5)),
            profile, PlanningChoices.Empty, Option.None<EffectivePlanningExtras>()).ValueOrThrow();
        Assert.Equal(Window(2027, 5, 6, 2027, 5, 15), result.Emergence);
        Assert.Equal(Option.Some(Window(2027, 6, 5, 2027, 6, 24)), result.Maturity);
        Assert.IsType<Option<DateWindow>.None>(result.Transplant);
        Assert.IsType<Option<DateWindow>.None>(result.PrickingOut);
        Assert.IsType<Option<DateWindow>.None>(result.HardeningStart);
        Assert.Equal(Window(2027, 5, 9, 2027, 5, 13), result.MeanReference);
    }

    [Fact]
    public void DirectSow_MissingVegetation_DoesNotBlockSowing()
    {
        var result = PlantingSchedule.Calculate(new GrowingMethod.DirectSow(Window(2027, 5, 1, 2027, 5, 1)),
            Profile(0, 0, 0, 0) with { SeedlingAge = Option.None<SourcedValue<DayRange>>() },
            PlanningChoices.Empty, Option.None<EffectivePlanningExtras>()).ValueOrThrow();
        Assert.Equal(result.Sowing, result.Emergence);
        Assert.IsType<Option<DateWindow>.None>(result.Maturity);
        Assert.Contains(result.Diagnostics, x => x.Code == PlanningDiagnosticCode.VegetationMissing);
    }

    [Fact]
    public void ManualSowing_OutsideWindow_IsPreservedAndDiagnosed()
    {
        var choices = PlanningChoices.Empty with { SowingDate = Option.Some(new DateOnly(2027, 1, 1)) };
        var result = PlantingSchedule.Calculate(new GrowingMethod.Transplant(Window(2027, 5, 20, 2027, 5, 20)),
            Profile(5, 10, 50, 60), choices, Option.None<EffectivePlanningExtras>()).ValueOrThrow();
        Assert.Equal(Window(2027, 1, 6, 2027, 1, 11), result.Emergence);
        Assert.Contains(result.Diagnostics, x => x.Code == PlanningDiagnosticCode.ManualSowingOutsideWindow);
        Assert.Contains(result.Diagnostics, x => x.Code == PlanningDiagnosticCode.IncompatibleAge);
        Assert.Equal(Option.Some(new DateOnly(2027, 1, 1)), choices.SowingDate);
    }

    [Fact]
    public void Operations_StayInsideSeedlingAge_AndUnknownDiffersFromDisabled()
    {
        var extras = GrowingProfileResolver.ResolveExtras(new PlanningExtras
        {
            PrickingOut = new OperationOverride<DayRange>.Enabled(Range(10, 15)),
            Hardening = new OperationOverride<DayRange>.Enabled(Range(7, 7))
        }, new(), new());
        var target = Window(2027, 5, 20, 2027, 5, 20);
        var result = PlantingSchedule.Calculate(new GrowingMethod.Transplant(target), Profile(5, 10, 50, 60),
            PlanningChoices.Empty with { SowingDate = Option.Some(new DateOnly(2027, 3, 20)) }, Option.Some(extras)).ValueOrThrow();
        Assert.Equal(Window(2027, 3, 11, 2027, 3, 26), result.Sowing);
        Assert.Equal(Option.Some(Window(2027, 4, 4, 2027, 4, 14)), result.PrickingOut);
        Assert.Equal(Option.Some(Window(2027, 5, 13, 2027, 5, 13)), result.HardeningStart);
        Assert.DoesNotContain(result.Diagnostics, x => x.Code == PlanningDiagnosticCode.StageOrder);
    }

    [Fact]
    public void Candidate_ReadinessIntersectionIncludesSingleBoundaryDay()
    {
        var candidate = PlantingSchedule.EvaluateCandidate(new DateOnly(2027, 3, 20),
            Window(2027, 5, 24, 2027, 5, 24), Range(5, 5), Range(60, 60)).ValueOrThrow();
        Assert.Equal(Option.Some(Window(2027, 5, 24, 2027, 5, 24)), candidate.AllowedTransplant);
        Assert.Equal(60, candidate.AgeAtTarget.Min);
        Assert.Equal(60, candidate.AgeAtTarget.Max);
    }

    [Fact]
    public void CalendarOverflowOrMissingAge_ReturnsFailure()
    {
        var profile = Profile(1, int.MaxValue, 1, int.MaxValue);
        var window = DateWindow.Create(DateOnly.MinValue, DateOnly.MinValue).ValueOrThrow();
        Assert.True(PlantingSchedule.Calculate(new GrowingMethod.Transplant(window), profile, PlanningChoices.Empty,
            Option.None<EffectivePlanningExtras>()).IsFailure);
        window = DateWindow.Create(DateOnly.MaxValue, DateOnly.MaxValue).ValueOrThrow();
        Assert.True(PlantingSchedule.Calculate(new GrowingMethod.DirectSow(window), profile, PlanningChoices.Empty,
            Option.None<EffectivePlanningExtras>()).IsFailure);
        Assert.True(PlantingSchedule.Calculate(new GrowingMethod.Transplant(window),
            profile with { SeedlingAge = Option.None<SourcedValue<DayRange>>() }, PlanningChoices.Empty,
            Option.None<EffectivePlanningExtras>()).IsFailure);
    }

    [Fact]
    public void Calendar_CrossesYearAndLeapDayWithoutRoundingDates()
    {
        var target = Window(2027, 1, 10, 2027, 1, 10);
        var seedling = PlantingSchedule.Calculate(new GrowingMethod.Transplant(target), Profile(2, 2, 30, 30),
            PlanningChoices.Empty, Option.None<EffectivePlanningExtras>()).ValueOrThrow();
        Assert.Equal(Window(2026, 12, 9, 2026, 12, 9), seedling.Sowing);
        var direct = PlantingSchedule.Calculate(new GrowingMethod.DirectSow(Window(2028, 2, 28, 2028, 2, 28)),
            Profile(1, 2, 0, 0), PlanningChoices.Empty, Option.None<EffectivePlanningExtras>()).ValueOrThrow();
        Assert.Equal(Window(2028, 2, 29, 2028, 3, 1), direct.Emergence);
    }

    [Fact]
    public void DisabledOperations_HaveNoMissingWarnings_ImpossibleHardeningRemainsVisible()
    {
        var target = new GrowingMethod.Transplant(Window(2027, 5, 20, 2027, 5, 20));
        var profile = Profile(5, 10, 50, 60);
        var choices = PlanningChoices.Empty with { SowingDate = Option.Some(new DateOnly(2027, 3, 20)) };
        var disabled = GrowingProfileResolver.ResolveExtras(new PlanningExtras
        {
            PrickingOut = new OperationOverride<DayRange>.Disabled(),
            Hardening = new OperationOverride<DayRange>.Disabled()
        }, new(), new());
        var result = PlantingSchedule.Calculate(target, profile, choices, Option.Some(disabled)).ValueOrThrow();
        Assert.DoesNotContain(result.Diagnostics, x => x.Code is PlanningDiagnosticCode.UnknownPrickingOut or PlanningDiagnosticCode.UnknownHardening);
        var conflict = disabled with { Hardening = new OperationPlan<DayRange>.Enabled(Source(100, 100)) };
        result = PlantingSchedule.Calculate(target, profile, choices, Option.Some(conflict)).ValueOrThrow();
        Assert.Contains(result.Diagnostics, x => x.Code == PlanningDiagnosticCode.StageOrder);
        Assert.Equal(Option.Some(Window(2027, 2, 9, 2027, 2, 9)), result.HardeningStart);
    }

    private static DayRange Range(int min, int max) => DayRange.Create(min, max).ValueOrThrow();
    private static SourcedValue<DayRange> Source(int min, int max) => new(Range(min, max), ParameterSource.Culture);
    private static EffectiveGrowingProfile Profile(int gmin, int gmax, int amin, int amax)
        => new(Source(gmin, gmax), Option.Some(Source(amin, amax)), Option.None<SourcedValue<DayRange>>());
    private static DateWindow Window(int y, int m, int d, int ey, int em, int ed)
        => DateWindow.Create(new(y, m, d), new(ey, em, ed)).ValueOrThrow();
}
