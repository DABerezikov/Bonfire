using MoonCalendar;

namespace Models.Tests;

// Контрольные результаты текущей реализации; не доказательство астрономической точности.
public class MoonCalendarBehaviorTests
{
    private readonly MoonPhase _moon = new();

    [Theory]
    [InlineData(1, "Растущая луна")]
    [InlineData(2, "Полнолуние")]
    [InlineData(6, "Убывающая луна")]
    [InlineData(9, "Третья четверть")]
    [InlineData(13, "Убывающий месяц")]
    [InlineData(17, "Новолуние")]
    [InlineData(20, "Растущий серп")]
    [InlineData(24, "Первая четверть")]
    public void GetMoonPhase_ReferenceDates_PreservesAllEightPhases(int day, string expected)
        => Assert.Equal(expected, _moon.GetMoonPhase(new DateTime(2026, 1, day)));

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
    [InlineData(2000, 1, 6, 29.43000199395109, 63.541423134086536, 1.8348313220404557, 281.83868519511236)]
    [InlineData(2026, 10, 24, 13.27482494574866, 58.94175512494149, 3.7777462762557152, 8.426690619373627)]
    [InlineData(2024, 2, 29, 19.7841272937928, 62.83424372013422, -1.8925704251809559, 213.68844717803094)]
    public void LunarCalculations_ReferenceDates_PreserveNumericResults(int year, int month, int day, double age, double distance, double latitude, double longitude)
    {
        var date = new DateTime(year, month, day);
        Assert.Equal(age, _moon.GetMoonAge(date), 8);
        Assert.Equal(distance, _moon.GetMoonDistance(date), 8);
        Assert.Equal(latitude, _moon.GetMoonLatitude(date), 8);
        Assert.Equal(longitude, _moon.GetMoonLongitude(date), 8);
        Assert.Equal(age, _moon.GetMoonAge(date.AddHours(23)), 8);
    }

    [Fact]
    public void CurrentProperties_MatchExplicitDateCalculations()
    {
        var before = DateTime.Now;
        var age = MoonPhase.MoonAge;
        var phase = _moon.LunarPhase;
        var distance = _moon.MoonDistance;
        var latitude = _moon.MoonLatitude;
        var longitude = _moon.MoonLongitude;
        var zodiac = _moon.Zodiac;
        var after = DateTime.Now;
        // Даже при переходе через полночь результат должен соответствовать одной из двух дат.
        Assert.Contains(age, new[] { _moon.GetMoonAge(before), _moon.GetMoonAge(after) });
        Assert.Contains(phase, new[] { _moon.GetMoonPhase(before), _moon.GetMoonPhase(after) });
        Assert.Contains(distance, new[] { _moon.GetMoonDistance(before), _moon.GetMoonDistance(after) });
        Assert.Contains(latitude, new[] { _moon.GetMoonLatitude(before), _moon.GetMoonLatitude(after) });
        Assert.Contains(longitude, new[] { _moon.GetMoonLongitude(before), _moon.GetMoonLongitude(after) });
        Assert.Contains(zodiac, new[] { _moon.GetZodiac(before), _moon.GetZodiac(after) });
    }
}
