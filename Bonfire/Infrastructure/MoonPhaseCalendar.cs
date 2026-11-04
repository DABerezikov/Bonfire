using System;
using Bonfire.Application.Seedlings;
using MoonCalendar;

namespace Bonfire.Infrastructure;

internal sealed class MoonPhaseCalendar(ILunarPhaseCalculator calculator) : ILunarCalendar
{
    public string GetPhase(DateTime date) => LunarPhaseNames.FromAngle(
        calculator.GetPhaseAngle(new DateTimeOffset(date.ToUniversalTime())));
}