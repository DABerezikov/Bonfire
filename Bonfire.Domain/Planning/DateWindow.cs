using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Planning;

/// <summary>Включительный календарный интервал, не ограниченный одним сезоном.</summary>
public sealed record DateWindow
{
    private DateWindow(DateOnly start, DateOnly end) => (Start, End) = (start, end);
    public DateOnly Start { get; }
    public DateOnly End { get; }

    public static Result<DateWindow> Create(DateOnly start, DateOnly end) => start <= end
        ? Result.Success(new DateWindow(start, end))
        : Result.Failure<DateWindow>("Начало периода позже окончания");

    public Result<DateWindow> Shift(long startDays, long endDays)
    {
        // Сравнение до преобразования в int исключает переполнение и тихий перенос.
        if (startDays < -Start.DayNumber || startDays > DateOnly.MaxValue.DayNumber - Start.DayNumber
                                         || endDays < -End.DayNumber ||
                                         endDays > DateOnly.MaxValue.DayNumber - End.DayNumber)
            return Result.Failure<DateWindow>("Расчёт выходит за границы календаря");
        var start = Start.DayNumber + startDays;
        var end = End.DayNumber + endDays;
        return Create(DateOnly.FromDayNumber((int)start), DateOnly.FromDayNumber((int)end));
    }

    public Option<DateWindow> Intersect(DateWindow other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var start = Start > other.Start ? Start : other.Start;
        var end = End < other.End ? End : other.End;
        return start <= end ? Option.Some(new DateWindow(start, end)) : Option.None<DateWindow>();
    }

    public bool Contains(DateOnly date) => date >= Start && date <= End;
}