using MoonCalendar;
using Bonfire.Infrastructure;
using Bonfire.Domain.Planning;

namespace Services.Tests;

public class LunarTrendCalendarTests
{
    [Theory]
    [InlineData(1, 7, LunarTrend.Waning)]
    [InlineData(1, 8, LunarTrend.Growing)]
    [InlineData(1, 22, LunarTrend.Growing)]
    [InlineData(1, 23, LunarTrend.Waning)]
    [InlineData(2, 6, LunarTrend.Waning)]
    [InlineData(2, 7, LunarTrend.Growing)]
    [InlineData(2, 20, LunarTrend.Growing)]
    [InlineData(2, 21, LunarTrend.Waning)]
    [InlineData(7, 3, LunarTrend.Waning)]
    [InlineData(7, 4, LunarTrend.Growing)]
    [InlineData(1, 16, LunarTrend.Growing)]
    [InlineData(1, 30, LunarTrend.Waning)]
    public void GetTrend_UsnoReferenceAtTomskNoon_ClassifiesWithoutForbiddenDays(int month, int day, LunarTrend expected)
    {
        var calendar = new LunarTrendCalendar(new AstronomyLunarPhaseCalculator());
        Assert.Equal(expected, calendar.GetTrend(new DateOnly(2027, month, day), TimeZoneInfo.FindSystemTimeZoneById("Asia/Tomsk")).ValueOrThrow());
    }

    [Fact]
    public void GetTrend_SameLocalDateInDifferentZones_CanHaveDifferentTrend()
    {
        var calendar = new LunarTrendCalendar(new AstronomyLunarPhaseCalculator());
        var day = new DateOnly(2027, 2, 20);
        Assert.Equal(LunarTrend.Growing, calendar.GetTrend(day, TimeZoneInfo.FindSystemTimeZoneById("Asia/Tomsk")).ValueOrThrow());
        var west = TimeZoneInfo.CreateCustomTimeZone("TestWest", TimeSpan.FromHours(-12), "West", "West");
        Assert.Equal(LunarTrend.Waning, calendar.GetTrend(day, west).ValueOrThrow());
    }

    [Fact]
    public void GetTrend_OutsideVerifiedYearRange_ReturnsFailure()
    {
        var calendar = new LunarTrendCalendar(new AstronomyLunarPhaseCalculator());
        Assert.True(calendar.GetTrend(DateOnly.MinValue, TimeZoneInfo.Utc).IsFailure);
        Assert.True(calendar.GetTrend(DateOnly.MaxValue, TimeZoneInfo.Utc).IsFailure);
    }

    [Fact]
    public void SharedCalculator_AfterUsnoNewMoon_ReportsElapsedAgeAndGrowingTrend()
    {
        var moon = new MoonPhase(new AstronomyLunarPhaseCalculator());
        // USNO: новолуние 06.02.2027 15:56 UT; спустя сутки возраст около 1.
        var moment = new DateTimeOffset(2027, 2, 7, 15, 56, 0, TimeSpan.Zero);
        Assert.InRange(moon.GetMoonAge(moment), 0.99, 1.01);
        Assert.InRange(moon.GetPhaseAngle(moment), 0, 180);
    }
}
