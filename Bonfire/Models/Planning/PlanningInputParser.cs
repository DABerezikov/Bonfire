using System;
using System.Globalization;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.Regions;

namespace Bonfire.Models.Planning;

public static class PlanningInputParser
{
    public static Result<PlantCount> Count(string text, string name)
        => int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value)
            ? PlantCount.Create(value) : Result.Failure<PlantCount>($"{name}: введите положительное целое число");

    public static Result<Option<PlantCount>> OptionalCount(string text, string name)
        => text.Length == 0 ? Result.Success(Option.None<PlantCount>()) : Count(text, name).Bind(value => Result.Success(Option.Some(value)));

    public static Result<DateOnly> Date(string text, string name)
        => DateOnly.TryParseExact(text, ["dd.MM.yyyy", "yyyy-MM-dd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? Result.Success(date) : Result.Failure<DateOnly>($"{name}: укажите дату дд.мм.гггг");

    public static Result<Option<DateOnly>> OptionalDate(string text, string name)
        => text.Length == 0 ? Result.Success(Option.None<DateOnly>()) : Date(text, name).Bind(value => Result.Success(Option.Some(value)));

    public static Result<DateWindow> Window(string start, string end)
        => Date(start, "Начало периода").Bind(first => Date(end, "Окончание периода").Bind(last => DateWindow.Create(first, last)));

    public static Result<Option<DayRange>> Range(string min, string max, string name)
    {
        if (min.Length == 0 && max.Length == 0) return Result.Success(Option.None<DayRange>());
        if (!int.TryParse(min, out var first) || !int.TryParse(max, out var last))
            return Result.Failure<Option<DayRange>>($"{name}: заполните обе целые границы либо оставьте обе пустыми");
        return DayRange.Create(first, last).Bind(value => Result.Success(Option.Some(value)));
    }

    public static Result<Option<decimal>> Decimal(string text, string name)
    {
        if (text.Length == 0) return Result.Success(Option.None<decimal>());
        return decimal.TryParse(text.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out var value)
            ? Result.Success(Option.Some(value)) : Result.Failure<Option<decimal>>($"{name}: введите число");
    }

    public static Result<Option<AnnualDateWindow>> Annual(string start, string end, bool nextYear)
    {
        if (start.Length == 0 && end.Length == 0) return Result.Success(Option.None<AnnualDateWindow>());
        return Date(start + ".2000", "Начало справочного периода дд.мм").Bind(first =>
            Date(end + ".2000", "Окончание справочного периода дд.мм").Bind(last =>
                AnnualDateWindow.Create(first.Month, first.Day, last.Month, last.Day, nextYear).Bind(value => Result.Success(Option.Some(value)))));
    }

    public static string Format(DateOnly date) => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
    public static string Format(Option<DateOnly> date) => date.Match(Format, () => "");
    public static string Error<T>(Result<T> result) => result.Match(_ => "", error => error);
}
