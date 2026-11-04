using System;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using MoonCalendar;

namespace Bonfire.Infrastructure;

internal sealed class LunarTrendCalendar(ILunarPhaseCalculator calculator) : ILunarTrendCalendar
{
    // Диапазон, в котором выполняем сверку с доступными эталонами USNO.
    public const int FirstSupportedYear = 1700;
    public const int LastSupportedYear = 2100;

    public Result<LunarTrend> GetTrend(DateOnly date, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        if (date.Year is < FirstSupportedYear or > LastSupportedYear)
            return Result.Failure<LunarTrend>(
                $"Лунный расчёт поддерживается для {FirstSupportedYear}–{LastSupportedYear} годов");

        var localNoon = date.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Unspecified);

        if (timeZone.IsInvalidTime(localNoon) || timeZone.IsAmbiguousTime(localNoon))
            return Result.Failure<LunarTrend>("Местный полдень неоднозначен или отсутствует в часовом поясе региона");

        var utc = TimeZoneInfo.ConvertTimeToUtc(localNoon, timeZone);

        var angle = calculator.GetPhaseAngle(new DateTimeOffset(utc));

        return double.IsFinite(angle) && angle is >= 0 and < 360
            ? Result.Success(angle < 180 ? LunarTrend.Growing : LunarTrend.Waning)
            : Result.Failure<LunarTrend>("Астрономический расчёт вернул недопустимый фазовый угол");
    }
}