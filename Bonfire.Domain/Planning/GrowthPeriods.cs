using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning.Regions;

namespace Bonfire.Domain.Planning;

public enum LifeCycleKind
{
    Annual,
    Biennial,
    Perennial
}

public enum GrowthPeriodOrigin
{
    FromEmergence,
    AnnualReference,
    Manual
}

public sealed record PhenologyOffsets
{
    public Option<DayRange> Start { get; init; } = Option.None<DayRange>();
    public Option<DayRange> End { get; init; } = Option.None<DayRange>();
    public bool IsValid() => Start.Match(start => End.Match(end => end.Max >= start.Min, () => true), () => true);
}

public sealed record LifeCycleProfile
{
    public LifeCycleKind Kind { get; init; }
    public int FirstFloweringSeason { get; init; } = 1;
    public int FirstHarvestSeason { get; init; } = 1;
    public PhenologyOffsets FloweringFirstYear { get; init; } = new();
    public PhenologyOffsets FruitingFirstYear { get; init; } = new();
    public Option<AnnualDateWindow> FloweringLater { get; init; } = Option.None<AnnualDateWindow>();
    public Option<AnnualDateWindow> FruitingLater { get; init; } = Option.None<AnnualDateWindow>();

    public Result<LifeCycleProfile> Validate()
    {
        var last = Kind == LifeCycleKind.Annual ? 1 : Kind == LifeCycleKind.Biennial ? 2 : int.MaxValue;
        return Enum.IsDefined(Kind) && FirstFloweringSeason > 0 && FirstHarvestSeason > 0 &&
               FirstFloweringSeason <= last && FirstHarvestSeason <= last &&
               FloweringFirstYear.IsValid() && FruitingFirstYear.IsValid()
            ? Result.Success(this)
            : Result.Failure<LifeCycleProfile>(
                "Неверный жизненный цикл, первый продуктивный сезон или порядок начала/конца периода");
    }
}

public sealed record GrowthPeriod(
    Option<DateWindow> Start,
    Option<DateWindow> End,
    Option<DateWindow> Envelope,
    GrowthPeriodOrigin Origin);

public abstract record SeasonalPeriod
{
    private SeasonalPeriod()
    {
    }

    public sealed record NotExpected(string Reason) : SeasonalPeriod;

    public sealed record Unknown(string Reason) : SeasonalPeriod;

    public sealed record Known(GrowthPeriod Period) : SeasonalPeriod;
}

public sealed record GrowthPeriods(SeasonalPeriod Flowering, SeasonalPeriod Fruiting)
{
    public static Result<GrowthPeriods> FirstYear(LifeCycleProfile profile, DateWindow emergence)
        => profile.Validate().Bind(_ => Result.Success(new GrowthPeriods(
            First(profile.FirstFloweringSeason, profile.FloweringFirstYear, Option.Some(emergence)),
            First(profile.FirstHarvestSeason, profile.FruitingFirstYear, Option.Some(emergence)))));

    internal static SeasonalPeriod First(int firstSeason, PhenologyOffsets offsets, Option<DateWindow> emergence)
    {
        if (firstSeason > 1)
            return new SeasonalPeriod.NotExpected($"Первый продуктивный сезон — {firstSeason}; в первом не ожидается");
        if (emergence is not Option<DateWindow>.Some known)
            return new SeasonalPeriod.Unknown("Всходы первого сезона не заданы");

        Result<Option<DateWindow>> Shift(Option<DayRange> days) => days.Match(range => known.Value
            .Shift(range.Min, range.Max)
            .Bind(window => Result.Success(Option.Some(window))), () => Result.Success(Option.None<DateWindow>()));

        return Shift(offsets.Start).Bind(start => Shift(offsets.End).Bind(end =>
        {
            if (start is Option<DateWindow>.None && end is Option<DateWindow>.None)
                return Result.Success<SeasonalPeriod>(
                    new SeasonalPeriod.Unknown("Дни начала и окончания от всходов не заданы"));
            var envelope = start.Match(first =>
                end.Match(last => Option.Some(DateWindow.Create(first.Start, last.End).ValueOrThrow()),
                    () => Option.None<DateWindow>()), () => Option.None<DateWindow>());
            return Result.Success<SeasonalPeriod>(
                new SeasonalPeriod.Known(new GrowthPeriod(start, end, envelope, GrowthPeriodOrigin.FromEmergence)));
        })).Match(value => value, error => new SeasonalPeriod.Unknown(error));
    }
}

public sealed record GrowthPeriodOverrides
{
    public Option<DateWindow> Flowering { get; init; } = Option.None<DateWindow>();
    public Option<DateWindow> Fruiting { get; init; } = Option.None<DateWindow>();
}

public sealed record SeasonalPhenology(int SeasonYear, int AgeSeason, GrowthPeriods Periods)
{
    public static Result<SeasonalPhenology> Project(LifeCycleProfile profile, int establishmentYear, int seasonYear,
        Option<DateWindow> firstEmergence, GrowthPeriodOverrides manual)
    {
        if (establishmentYear is < 1 or > 9999 || seasonYear is < 1 or > 9999 || seasonYear < establishmentYear)
            return Result.Failure<SeasonalPhenology>("Сезон должен быть не раньше года начала посадки");
        if (manual.Flowering.Match(x => x.Start.Year != seasonYear || x.End.Year > seasonYear + 1, () => false) ||
            manual.Fruiting.Match(x => x.Start.Year != seasonYear || x.End.Year > seasonYear + 1, () => false))
            return Result.Failure<SeasonalPhenology>(
                "Ручной период должен начинаться в выбранном сезоне и завершаться не позже следующего года");
        var age = seasonYear - establishmentYear + 1;
        var outside = (profile.Kind == LifeCycleKind.Annual && age > 1) ||
                      (profile.Kind == LifeCycleKind.Biennial && age > 2);
        if (outside && (manual.Flowering is Option<DateWindow>.Some || manual.Fruiting is Option<DateWindow>.Some))
            return Result.Failure<SeasonalPhenology>("Для продолжения посадки после окончания цикла измените профиль");
        return profile.Validate().Bind(_ => Result.Success(new SeasonalPhenology(seasonYear, age,
            new GrowthPeriods(
                Period(profile.FirstFloweringSeason, profile.FloweringFirstYear, profile.FloweringLater,
                    manual.Flowering),
                Period(profile.FirstHarvestSeason, profile.FruitingFirstYear, profile.FruitingLater,
                    manual.Fruiting)))));

        SeasonalPeriod Period(int first, PhenologyOffsets offsets, Option<AnnualDateWindow> annual,
            Option<DateWindow> chosen)
        {
            if (outside)
                return new SeasonalPeriod.NotExpected("Жизненный цикл завершён; автоматического продолжения нет");
            if (chosen is Option<DateWindow>.Some picked)
                return new SeasonalPeriod.Known(new GrowthPeriod(Option.None<DateWindow>(), Option.None<DateWindow>(),
                    chosen, GrowthPeriodOrigin.Manual));
            if (age < first) return new SeasonalPeriod.NotExpected($"Ожидается с сезона №{first}");
            if (age == 1) return GrowthPeriods.First(first, offsets, firstEmergence);
            return annual.Match(window => window.Project(seasonYear).Match<SeasonalPeriod>(dates =>
                    new SeasonalPeriod.Known(
                        new GrowthPeriod(Option.None<DateWindow>(), Option.None<DateWindow>(), Option.Some(dates),
                            GrowthPeriodOrigin.AnnualReference)), error => new SeasonalPeriod.Unknown(error)),
                () => new SeasonalPeriod.Unknown("Справочное окно последующих сезонов не задано"));
        }
    }
}