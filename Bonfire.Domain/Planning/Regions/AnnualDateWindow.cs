using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Planning.Regions;

/// <summary>Ежегодное справочное окно; переход года задаётся явно.</summary>
public sealed record AnnualDateWindow
{
    private AnnualDateWindow(int startMonth, int startDay, int endMonth, int endDay, bool endInNextYear)
        => (StartMonth, StartDay, EndMonth, EndDay, EndInNextYear) =
            (startMonth, startDay, endMonth, endDay, endInNextYear);

    public int StartMonth { get; }
    public int StartDay { get; }
    public int EndMonth { get; }
    public int EndDay { get; }
    public bool EndInNextYear { get; }

    public static Result<AnnualDateWindow> Create(int startMonth, int startDay, int endMonth, int endDay,
        bool endInNextYear)
    {
        if (!ValidDay(2000, startMonth, startDay) || !ValidDay(2000, endMonth, endDay))
            return Result.Failure<AnnualDateWindow>("Недопустимые месяц или день справочного периода");
        if (!endInNextYear && new DateOnly(2000, startMonth, startDay) > new DateOnly(2000, endMonth, endDay))
            return Result.Failure<AnnualDateWindow>(
                "Переход справочного периода через Новый год должен быть указан явно");
        return Result.Success(new AnnualDateWindow(startMonth, startDay, endMonth, endDay, endInNextYear));
    }

    public Result<DateWindow> Project(int year)
    {
        var endYear = (long)year + (EndInNextYear ? 1 : 0);
        if (year is < 1 or > 9999 || endYear > 9999
                                  || !ValidDay(year, StartMonth, StartDay) || !ValidDay((int)endYear, EndMonth, EndDay))
            return Result.Failure<DateWindow>(
                "Справочный период не существует в выбранном сезоне (год или 29 февраля)");
        return DateWindow.Create(new DateOnly(year, StartMonth, StartDay),
            new DateOnly((int)endYear, EndMonth, EndDay));
    }

    private static bool ValidDay(int year, int month, int day)
        => month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(year, month);
}