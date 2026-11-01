using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;

namespace Models.Tests;

public class LunarSowingTests
{
    [Theory]
    [InlineData(LunarGroup.AboveGround, LunarTrend.Growing)]
    [InlineData(LunarGroup.RootCrop, LunarTrend.Waning)]
    public void Recommend_BothGroups_FilterDaysWithoutOtherRestrictions(LunarGroup group, LunarTrend expected)
    {
        var result = Recommend(group, date => Result.Success(date.Day % 2 == 0 ? LunarTrend.Growing : LunarTrend.Waning));
        var available = Assert.IsType<SowingRecommendations.Available>(result);
        Assert.All(available.Candidates, x => Assert.Equal(expected, x.Trend));
        Assert.Equal(2, available.Candidates.Length);
    }

    [Fact]
    public void Recommend_UnspecifiedGroup_DoesNotReadCalendar()
    {
        var result = Recommend(LunarGroup.Unspecified, _ => throw new Exception("Не должен читать календарь"));
        Assert.Equal(LunarUnavailableReason.UnspecifiedGroup, Assert.IsType<SowingRecommendations.Unavailable>(result).Reason);
    }

    [Fact]
    public void Recommend_Conflict_ExplainsEarlierAndLaterDirectSowing()
    {
        var first = new DateOnly(2027, 5, 10);
        var result = Recommend(LunarGroup.AboveGround,
            date => Result.Success(date < first || date > first.AddDays(3) ? LunarTrend.Growing : LunarTrend.Waning));
        var conflict = Assert.IsType<SowingRecommendations.Conflict>(result);
        var earlier = conflict.Earlier.Match(x => x, () => throw new Exception());
        var later = conflict.Later.Match(x => x, () => throw new Exception());
        Assert.Equal(first.AddDays(-1), earlier.Date);
        Assert.Equal(-1, earlier.ShiftDays);
        Assert.Equal(first.AddDays(4), later.Date);
        Assert.Equal(1, later.ShiftDays);
        Assert.Equal(first.AddDays(4), earlier.Preview.Emergence.Start);
        Assert.IsType<Option<CandidateReadiness>.None>(earlier.Readiness);
    }

    [Fact]
    public void Recommend_LimitExhaustionDiffersFromCalendarFailure()
    {
        var conflict = Assert.IsType<SowingRecommendations.Conflict>(Recommend(LunarGroup.AboveGround,
            _ => Result.Success(LunarTrend.Waning), limit: 0));
        Assert.IsType<Option<SowingCandidate>.None>(conflict.Earlier);
        Assert.IsType<Option<SowingCandidate>.None>(conflict.Later);
        var unavailable = Assert.IsType<SowingRecommendations.Unavailable>(Recommend(LunarGroup.AboveGround,
            _ => Result.Failure<LunarTrend>("Нет эфемерид")));
        Assert.Equal(LunarUnavailableReason.CalendarFailure, unavailable.Reason);
        Assert.Contains("Нет эфемерид", unavailable.Message);
    }

    [Fact]
    public void Recommend_ManualDateIsAssessedAndNeverReplaced()
    {
        var chosen = new DateOnly(2027, 5, 1);
        var choices = PlanningChoices.Empty with { SowingDate = Option.Some(chosen) };
        var result = Recommend(LunarGroup.AboveGround, date => Result.Success(date == chosen ? LunarTrend.Waning : LunarTrend.Growing), choices);
        var available = Assert.IsType<SowingRecommendations.Available>(result);
        var manual = available.ManualChoice.Match(x => x, () => throw new Exception());
        Assert.Equal(chosen, manual.Date);
        Assert.False(manual.MatchesPreference);
        Assert.False(manual.InsideSowingWindow);
        Assert.Equal(Option.Some(chosen), choices.SowingDate);
    }

    [Fact]
    public void Recommend_Transplant_RanksResilientFirstAndExplainsAge()
    {
        var target = DateWindow.Create(new(2027, 5, 20), new(2027, 5, 20)).ValueOrThrow();
        var profile = new EffectiveGrowingProfile(Source(5, 10), Option.Some(Source(50, 60)), Option.None<SourcedValue<DayRange>>());
        var method = new GrowingMethod.Transplant(target);
        var result = SowingRecommendations.Calculate(method, profile, PlanningChoices.Empty, Option.None<EffectivePlanningExtras>(),
            LunarGroup.AboveGround, _ => Result.Success(LunarTrend.Growing), 35).ValueOrThrow();
        var available = Assert.IsType<SowingRecommendations.Available>(result);
        Assert.Equal(new DateOnly(2027, 3, 16), available.Candidates[0].Date);
        Assert.True(available.HasResilientCandidates);
        var candidate = available.Candidates[0].Readiness.Match(x => x, () => throw new Exception());
        Assert.Equal(55, candidate.AgeAtTarget.Min);
        Assert.Equal(60, candidate.AgeAtTarget.Max);
    }

    [Fact]
    public void Recommend_CalendarFailureOutsideWindow_DoesNotInvalidateAvailableDates()
    {
        var result = Recommend(LunarGroup.AboveGround, date => date.Day is >= 10 and <= 13
            ? Result.Success(LunarTrend.Growing) : Result.Failure<LunarTrend>("Вне окна"));
        Assert.IsType<SowingRecommendations.Available>(result);
    }

    [Fact]
    public void Recommend_MatchesOnlyOutsideResilientWindow_ReturnsWarningAndKeepsPossibleDates()
    {
        var target = DateWindow.Create(new(2027, 5, 20), new(2027, 5, 20)).ValueOrThrow();
        var profile = new EffectiveGrowingProfile(Source(5, 10), Option.Some(Source(50, 60)), Option.None<SourcedValue<DayRange>>());
        var result = SowingRecommendations.Calculate(new GrowingMethod.Transplant(target), profile, PlanningChoices.Empty,
            Option.None<EffectivePlanningExtras>(), LunarGroup.AboveGround,
            date => Result.Success(date == new DateOnly(2027, 3, 11) ? LunarTrend.Growing : LunarTrend.Waning), 35).ValueOrThrow();
        var available = Assert.IsType<SowingRecommendations.Available>(result);
        Assert.False(available.HasResilientCandidates);
        Assert.True(available.ResilientPreferenceMissed);
        Assert.Equal(new DateOnly(2027, 3, 11), Assert.Single(available.Candidates).Date);
    }

    [Fact]
    public void Recommend_ShiftedTransplantSowing_ShowsAgeAgainstOriginalTarget()
    {
        var target = DateWindow.Create(new(2027, 5, 20), new(2027, 5, 20)).ValueOrThrow();
        var profile = new EffectiveGrowingProfile(Source(5, 10), Option.Some(Source(50, 60)), Option.None<SourcedValue<DayRange>>());
        var result = SowingRecommendations.Calculate(new GrowingMethod.Transplant(target), profile, PlanningChoices.Empty,
            Option.None<EffectivePlanningExtras>(), LunarGroup.AboveGround,
            date => Result.Success(date < new DateOnly(2027, 3, 11) || date > new DateOnly(2027, 3, 26)
                ? LunarTrend.Growing : LunarTrend.Waning), 35).ValueOrThrow();
        var conflict = Assert.IsType<SowingRecommendations.Conflict>(result);
        var earlier = conflict.Earlier.Match(x => x, () => throw new Exception());
        var later = conflict.Later.Match(x => x, () => throw new Exception());
        Assert.Equal(new AgeBounds(61, 66), earlier.Readiness.Match(x => x.AgeAtTarget, () => throw new Exception()));
        Assert.Equal(new AgeBounds(44, 49), later.Readiness.Match(x => x.AgeAtTarget, () => throw new Exception()));
        Assert.Contains(earlier.Preview.Diagnostics, x => x.Code == PlanningDiagnosticCode.IncompatibleAge);
    }

    [Fact]
    public void Recommend_IncompleteOrInvalidCalendar_IsUnavailableInsteadOfConflict()
    {
        Assert.IsType<SowingRecommendations.Unavailable>(Recommend(LunarGroup.AboveGround, date => date.Day == 11
            ? Result.Failure<LunarTrend>("День отсутствует") : Result.Success(LunarTrend.Growing)));
        Assert.IsType<SowingRecommendations.Unavailable>(Recommend(LunarGroup.AboveGround, _ => Result.Success((LunarTrend)99)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Recommend_CalendarBoundary_DoesNotWrapOrOverflowSearch(bool lastDay)
    {
        var day = lastDay ? DateOnly.MaxValue : DateOnly.MinValue;
        var method = new GrowingMethod.DirectSow(DateWindow.Create(day, day).ValueOrThrow());
        var profile = new EffectiveGrowingProfile(Source(0, 0), Option.None<SourcedValue<DayRange>>(), Option.None<SourcedValue<DayRange>>());
        var result = SowingRecommendations.Calculate(method, profile, PlanningChoices.Empty, Option.None<EffectivePlanningExtras>(),
            LunarGroup.AboveGround, date => Result.Success(date == day ? LunarTrend.Waning : LunarTrend.Growing), int.MaxValue).ValueOrThrow();
        var conflict = Assert.IsType<SowingRecommendations.Conflict>(result);
        Assert.IsType<Option<SowingCandidate>.None>(lastDay ? conflict.Later : conflict.Earlier);
    }

    [Fact]
    public void Recommend_NegativeSearchLimit_IsRejectedBeforeCalendarRead()
    {
        var method = new GrowingMethod.DirectSow(DateWindow.Create(new(2027, 5, 1), new(2027, 5, 1)).ValueOrThrow());
        var profile = new EffectiveGrowingProfile(Source(1, 1), Option.None<SourcedValue<DayRange>>(), Option.None<SourcedValue<DayRange>>());
        Assert.True(SowingRecommendations.Calculate(method, profile, PlanningChoices.Empty, Option.None<EffectivePlanningExtras>(),
            LunarGroup.AboveGround, _ => throw new Exception("Не должен читать календарь"), -1).IsFailure);
    }

    private static SowingRecommendations Recommend(LunarGroup group, Func<DateOnly, Result<LunarTrend>> calendar,
        PlanningChoices? choices = null, int limit = 35)
        => SowingRecommendations.Calculate(new GrowingMethod.DirectSow(DateWindow.Create(new(2027, 5, 10), new(2027, 5, 13)).ValueOrThrow()),
            new EffectiveGrowingProfile(Source(5, 10), Option.None<SourcedValue<DayRange>>(), Option.None<SourcedValue<DayRange>>()),
            choices ?? PlanningChoices.Empty, Option.None<EffectivePlanningExtras>(), group, calendar, limit).ValueOrThrow();

    private static SourcedValue<DayRange> Source(int min, int max) => new(DayRange.Create(min, max).ValueOrThrow(), ParameterSource.Culture);
}
