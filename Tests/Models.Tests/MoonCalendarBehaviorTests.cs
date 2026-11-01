using MoonCalendar;

namespace Models.Tests;

// Фазы сверены с USNO; прочие приближённые функции сохраняют прежние результаты.
public class MoonCalendarBehaviorTests
{
    private readonly MoonPhase _moon = new(new Bonfire.Infrastructure.AstronomyLunarPhaseCalculator());

    [Theory]
    [InlineData(1, 7, "Новолуние")]
    [InlineData(1, 11, "Растущий серп")]
    [InlineData(1, 16, "Первая четверть")]
    [InlineData(1, 19, "Растущая луна")]
    [InlineData(1, 22, "Полнолуние")]
    [InlineData(1, 26, "Убывающая луна")]
    [InlineData(1, 30, "Третья четверть")]
    [InlineData(2, 2, "Убывающий месяц")]
    public void GetMoonPhase_UsnoCycle_PreservesEightNamesWithAccurateAngles(int month, int day, string expected)
        => Assert.Equal(expected, _moon.GetMoonPhase(new DateTime(2027, month, day, 12, 0, 0, DateTimeKind.Utc)));

    [Theory]
    [InlineData(2027, 2, 6, 15, 56, 0)]
    [InlineData(2027, 2, 20, 23, 23, 180)]
    [InlineData(2027, 7, 4, 3, 2, 0)]
    public void GetPhaseAngle_UsnoEventWithinTenMinutes_CrossesExpectedBoundary(int year, int month, int day, int hour, int minute, int boundary)
    {
        var moment = new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero);
        var before = _moon.GetPhaseAngle(moment.AddMinutes(-10));
        var after = _moon.GetPhaseAngle(moment.AddMinutes(10));
        if (boundary == 0)
        {
            Assert.InRange(before, 350, 360);
            Assert.InRange(after, 0, 10);
        }
        else
        {
            Assert.True(before < boundary);
            Assert.True(after > boundary);
        }
    }

    [Theory]
    [InlineData(1, "Телец")]
    [InlineData(3, "Близнецы")]
    [InlineData(5, "Рак")]
    [InlineData(6, "Лев")]
    [InlineData(9, "Дева")]
    [InlineData(13, "Весы")]
    [InlineData(14, "Скорпион")]
    [InlineData(17, "Стрелец")]
    [InlineData(19, "Козерог")]
    [InlineData(20, "Водолей")]
    [InlineData(23, "Рыбы")]
    [InlineData(26, "Овен")]
    public void GetZodiac_ReferenceDates_PreservesAllTwelveSigns(int day, string expected)
        => Assert.Equal(expected, _moon.GetZodiac(new DateTime(2026, 1, day)));

    [Theory]
    [InlineData(2000, 1, 6, 63.541423134086536, 1.8348313220404557, 281.83868519511236)]
    [InlineData(2026, 10, 24, 58.94175512494149, 3.7777462762557152, 8.426690619373627)]
    [InlineData(2024, 2, 29, 62.83424372013422, -1.8925704251809559, 213.68844717803094)]
    public void NonPhaseCalculations_ReferenceDates_PreserveNumericResults(int year, int month, int day, double distance, double latitude, double longitude)
    {
        var date = new DateTime(year, month, day);
        Assert.Equal(distance, _moon.GetMoonDistance(date), 8);
        Assert.Equal(latitude, _moon.GetMoonLatitude(date), 8);
        Assert.Equal(longitude, _moon.GetMoonLongitude(date), 8);
    }

    [Fact]
    public void CurrentProperties_MatchExplicitDateCalculations()
    {
        var before = DateTime.Now;
        var age = _moon.MoonAge;
        var phase = _moon.LunarPhase;
        var distance = _moon.MoonDistance;
        var latitude = _moon.MoonLatitude;
        var longitude = _moon.MoonLongitude;
        var zodiac = _moon.Zodiac;
        var after = DateTime.Now;
        // Даже при переходе через полночь результат должен соответствовать одной из двух дат.
        var ageBefore = _moon.GetMoonAge(before);
        var ageAfter = _moon.GetMoonAge(after);
        Assert.True(ageBefore <= ageAfter ? age >= ageBefore && age <= ageAfter : age >= ageBefore || age <= ageAfter);
        Assert.Contains(phase, new[] { _moon.GetMoonPhase(before), _moon.GetMoonPhase(after) });
        Assert.Contains(distance, new[] { _moon.GetMoonDistance(before), _moon.GetMoonDistance(after) });
        Assert.Contains(latitude, new[] { _moon.GetMoonLatitude(before), _moon.GetMoonLatitude(after) });
        Assert.Contains(longitude, new[] { _moon.GetMoonLongitude(before), _moon.GetMoonLongitude(after) });
        Assert.Contains(zodiac, new[] { _moon.GetZodiac(before), _moon.GetZodiac(after) });
    }
}
