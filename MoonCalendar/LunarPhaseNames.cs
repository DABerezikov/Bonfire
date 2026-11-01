namespace MoonCalendar;

/// <summary>Общее представление фазового угла восемью прежними названиями.</summary>
public static class LunarPhaseNames
{
    public static string FromAngle(double angle)
    {
        if (!double.IsFinite(angle) || angle is < 0 or >= 360)
            throw new ArgumentOutOfRangeException(nameof(angle), "Фазовый угол должен быть в диапазоне [0;360)");
        return angle switch
        {
            < 22.5 => "Новолуние",
            < 67.5 => "Растущий серп",
            < 112.5 => "Первая четверть",
            < 157.5 => "Растущая луна",
            < 202.5 => "Полнолуние",
            < 247.5 => "Убывающая луна",
            < 292.5 => "Третья четверть",
            < 337.5 => "Убывающий месяц",
            _ => "Новолуние"
        };
    }
}
