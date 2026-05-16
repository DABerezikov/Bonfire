namespace MoonCalendar;

public class MoonPhase
{
    /// <summary> Текущий лунный день </summary>
    public static double MoonAge {
        get
        {
            var julianDay = CalculateJulianDay(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            return CalculateMoonAge(julianDay);
        }
    }

    /// <summary> Текущая фаза Луны </summary>
    public string LunarPhase => MoonPhaseCalc(MoonAge);

    /// <summary> Текущее расстояние до Луны </summary>
    public double MoonDistance => GetMoonDistance(DateTime.Now);

    /// <summary> Текущая широта затмения Луны </summary>
    public double MoonLatitude => GetMoonLatitude(DateTime.Now);

    /// <summary> Текущая долгота затмения Луны </summary>
    public double MoonLongitude => GetMoonLongitude(DateTime.Now);

    /// <summary> Текущее созвездие </summary>
    public string Zodiac => GetZodiac(MoonLongitude);

    /// <summary>Лунный день в указанную дату </summary>
    public double GetMoonAge(DateTime date)
    {
        var julianDay = CalculateJulianDay(date.Year, date.Month, date.Day);
        return CalculateMoonAge(julianDay);
    }

    /// <summary>Фаза Луны в указанную дату </summary>
    public string GetMoonPhase(DateTime date)
    {
        var moonAge = GetMoonAge(date);
        return MoonPhaseCalc(moonAge);
    }

    /// <summary>Расстояние до Луны в указанную дату</summary>
    public double GetMoonDistance(DateTime date)
    {
        var year = date.Year;
        var month = date.Month;
        var day = date.Day;
        var julianDay = CalculateJulianDay(year, month, day);
        var synodicMonthRadian = Normalize((julianDay - 2451550.1) / 29.530588853) * 2 * Math.PI;
        var anomalyMonthRadian = 2 * Math.PI * Normalize((julianDay - 2451562.2) / 27.55454988);
        return 60.4 - 3.3 * Math.Cos(anomalyMonthRadian) -
            0.6 * Math.Cos(2 * synodicMonthRadian - anomalyMonthRadian) -
            0.5 * Math.Cos(2 * synodicMonthRadian);
    }

    /// <summary>Широта затмения Луны в указанную дату</summary>
    public double GetMoonLatitude(DateTime date)
    {
        var year = date.Year;
        var month = date.Month;
        var day = date.Day;
        var julianDay = CalculateJulianDay(year, month, day);
        var draconicMonthRadian = 2 * Math.PI * Normalize((julianDay - 2451565.2) / 27.212220817);
        return 5.1 * Math.Sin(draconicMonthRadian);
    }

    /// <summary>Долгота затмения Луны в указанную дату</summary>
    public double GetMoonLongitude(DateTime date)
    {
        var year = date.Year;
        var month = date.Month;
        var day = date.Day;
        var julianDay = CalculateJulianDay(year, month, day);
        var synodicMonthRadian = Normalize((julianDay - 2451550.1) / 29.530588853) * 2 * Math.PI;
        var anomalyMonthRadian = 2 * Math.PI * Normalize((julianDay - 2451562.2) / 27.55454988);
        var tropicalMonthRadian = Normalize((julianDay - 2451555.8) / 27.321582241);
        return 360 * tropicalMonthRadian + 6.3 * Math.Sin(anomalyMonthRadian) +
            1.3 * Math.Sin(2 * synodicMonthRadian - anomalyMonthRadian) +
            0.7 * Math.Sin(2 * synodicMonthRadian);
    }

    /// <summary>Знак зодиака Луны в указанную дату</summary>
    public string GetZodiac(DateTime date)
    {
        var moonLongitude = GetMoonLongitude(date);
        return GetZodiac(moonLongitude);
    }

    private string GetZodiac(double moonLongitude)
    {
        return moonLongitude switch
        {
            < 33.18 => "Рыбы",
            < 51.16 => "Овен",
            < 93.44 => "Телец",
            < 119.48 => "Близнецы",
            < 135.3 => "Рак",
            < 173.34 => "Лев",
            < 224.17 => "Дева",
            < 242.57 => "Весы",
            < 271.26 => "Скорпион",
            < 302.49 => "Стрелец",
            < 311.72 => "Козерог",
            < 348.58 => "Водолей",
            _ => "Рыбы"
        };
    }

    private static double CalculateMoonAge(int julianDay)
    {
        return Normalize((julianDay - 2451550.1) / 29.530588853) * 29.53;
    }

    private static int CalculateJulianDay(int year, int month, int day)
    {
        var julianYear = year - (int)Math.Floor((12.0 - month) / 10.0);
        var julianMonth = month + 9;
        if (julianMonth >= 12)
            julianMonth -= 12;
        var factor1 = (int)Math.Floor(365.25 * (julianYear + 4712.0));
        var factor2 = (int)Math.Floor(30.6 * julianMonth + 0.5);
        var factor3 = (int)Math.Floor((julianYear / 100.0 + 49.0) * 0.75) - 38;
        var julianDay = factor1 + factor2 + day + 59;
        if (julianDay > 2299160)
            julianDay -= factor3;
        return julianDay;
    }

    private static string MoonPhaseCalc(double moonsAge)
    {
        return moonsAge switch
        {
            < 1.84566 => "Новолуние",
            < 5.53699 => "Растущий серп",
            < 9.22831 => "Первая четверть",
            < 12.91963 => "Растущая луна",
            < 16.61096 => "Полнолуние",
            < 20.30228 => "Убывающая луна",
            < 23.99361 => "Третья четверть",
            < 27.68493 => "Убывающий месяц",
            _ => "Новолуние"
        };
    }

    private static double Normalize(double v)
    {
        v -= Math.Floor(v);
        if (v < 0)
            v += 1;
        return v;
    }
}
