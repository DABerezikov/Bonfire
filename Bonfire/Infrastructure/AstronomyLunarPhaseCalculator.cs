using System;
using CosineKitty;
using MoonCalendar;

namespace Bonfire.Infrastructure;

/// <summary>Единственная реализация источника фаз; конкретная библиотека изолирована здесь.</summary>
internal sealed class AstronomyLunarPhaseCalculator : ILunarPhaseCalculator
{
    public double GetPhaseAngle(DateTimeOffset moment)
        => Astronomy.MoonPhase(new AstroTime(moment.UtcDateTime));

    public double GetMoonAge(DateTimeOffset moment)
    {
        var time = new AstroTime(moment.UtcDateTime);
        var newMoon = Astronomy.SearchMoonPhase(0, time, -40)
            ?? throw new InvalidOperationException("Астрономический расчёт не нашёл предыдущее новолуние");
        return time.ut - newMoon.ut;
    }
}
