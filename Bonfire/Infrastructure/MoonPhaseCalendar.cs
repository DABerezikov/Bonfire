using System;
using Bonfire.Application.Seedlings;
using MoonCalendar;

namespace Bonfire.Infrastructure;

internal sealed class MoonPhaseCalendar(MoonPhase phase) : ILunarCalendar
{
    public string GetPhase(DateTime date) => phase.GetMoonPhase(date);
}
