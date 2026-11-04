using MoonCalendar;

namespace Models.Tests;

public class MoonPhaseDependencyTests
{
    [Fact]
    public void MoonPhase_InjectedCalculator_ControlsPhaseAndAgeWithoutConcreteProvider()
    {
        var calendar = new MoonPhase(new TestCalculator());
        var instant = new DateTimeOffset(2027, 5, 1, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(90, calendar.GetPhaseAngle(instant));
        Assert.Equal(4.25, calendar.GetMoonAge(instant));
        Assert.Equal("Первая четверть", calendar.GetMoonPhase(instant.UtcDateTime));
    }

    private sealed class TestCalculator : ILunarPhaseCalculator
    {
        public double GetPhaseAngle(DateTimeOffset moment) => 90;
        public double GetMoonAge(DateTimeOffset moment) => 4.25;
    }
}
