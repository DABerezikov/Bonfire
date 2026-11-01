using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;

namespace Services.Tests;

public class SowingRecommendationApplicationTests
{
    private readonly ILunarTrendCalendar _calendar = Substitute.For<ILunarTrendCalendar>();

    [Fact]
    public void Execute_ProviderFailure_KeepsAgronomyAndFailureReason()
    {
        _calendar.GetTrend(Arg.Any<DateOnly>(), Arg.Any<TimeZoneInfo>()).Returns(Result.Failure<LunarTrend>("Нет данных"));
        var preview = new RecommendSowing(_calendar).Execute(Request()).ValueOrThrow();
        Assert.Equal(new DateOnly(2027, 5, 15), preview.Agronomy.Emergence.Start);
        Assert.Contains("Нет данных", Assert.IsType<SowingRecommendations.Unavailable>(preview.Lunar).Message);
    }

    [Fact]
    public void Execute_ManualDayInsideWindow_ReadsEachDayOnceAndPreservesInput()
    {
        _calendar.GetTrend(Arg.Any<DateOnly>(), Arg.Any<TimeZoneInfo>()).Returns(Result.Success(LunarTrend.Growing));
        var request = Request() with { Choices = PlanningChoices.Empty with { SowingDate = Option.Some(new DateOnly(2027, 5, 10)) } };
        var preview = new RecommendSowing(_calendar).Execute(request).ValueOrThrow();
        Assert.IsType<SowingRecommendations.Available>(preview.Lunar);
        _calendar.Received(1).GetTrend(new DateOnly(2027, 5, 10), request.TimeZone);
        Assert.Equal(2, _calendar.ReceivedCalls().Count());
        Assert.Equal(Option.Some(new DateOnly(2027, 5, 10)), request.Choices.SowingDate);
    }

    [Fact]
    public void Execute_UnspecifiedGroup_DoesNotReadCalendar()
    {
        var preview = new RecommendSowing(_calendar).Execute(Request() with { Group = LunarGroup.Unspecified }).ValueOrThrow();
        Assert.IsType<SowingRecommendations.Unavailable>(preview.Lunar);
        Assert.Empty(_calendar.ReceivedCalls());
    }

    [Fact]
    public void Execute_CancellationBeforeOrDuringCalculation_Propagates()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => new RecommendSowing(_calendar).Execute(Request(), cancellation.Token));
        Assert.Empty(_calendar.ReceivedCalls());
        using var during = new CancellationTokenSource();
        _calendar.GetTrend(Arg.Any<DateOnly>(), Arg.Any<TimeZoneInfo>()).Returns(_ =>
        {
            during.Cancel();
            return Result.Success(LunarTrend.Growing);
        });
        Assert.ThrowsAny<OperationCanceledException>(() => new RecommendSowing(_calendar).Execute(Request(), during.Token));
    }

    private static SowingRecommendationRequestFromViewModel Request() => new(
        new GrowingMethod.DirectSow(DateWindow.Create(new(2027, 5, 10), new(2027, 5, 11)).ValueOrThrow()),
        new EffectiveGrowingProfile(new(DayRange.Create(5, 10).ValueOrThrow(), ParameterSource.Culture),
            Option.None<SourcedValue<DayRange>>(), Option.None<SourcedValue<DayRange>>()),
        PlanningChoices.Empty, Option.None<EffectivePlanningExtras>(), LunarGroup.AboveGround,
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Tomsk"), 35);
}
