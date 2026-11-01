namespace MoonCalendar;

/// <summary>Порт единого источника расчёта фаз; не зависит от астрономической библиотеки.</summary>
public interface ILunarPhaseCalculator
{
    double GetPhaseAngle(DateTimeOffset moment);
    double GetMoonAge(DateTimeOffset moment);
}