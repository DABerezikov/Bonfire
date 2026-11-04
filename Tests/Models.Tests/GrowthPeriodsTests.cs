using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.Regions;

namespace Models.Tests;

public class GrowthPeriodsTests
{
    [Fact]
    public void FirstYear_KeepsIndependentStartEndAndDoesNotInventDuration()
    {
        var profile = new LifeCycleProfile { Kind = LifeCycleKind.Perennial, FirstHarvestSeason = 2,
            FloweringFirstYear = new() { Start = Option.Some(DayRange.Create(30, 40).ValueOrThrow()) } };
        var emergence = DateWindow.Create(new(2027, 3, 1), new(2027, 3, 5)).ValueOrThrow();
        var result = GrowthPeriods.FirstYear(profile, emergence).ValueOrThrow();
        var flowering = Assert.IsType<SeasonalPeriod.Known>(result.Flowering).Period;
        Assert.Equal(Option.Some(DateWindow.Create(new(2027, 3, 31), new(2027, 4, 14)).ValueOrThrow()), flowering.Start);
        Assert.IsType<Option<DateWindow>.None>(flowering.End); Assert.IsType<Option<DateWindow>.None>(flowering.Envelope);
        Assert.IsType<SeasonalPeriod.NotExpected>(result.Fruiting);
    }

    [Theory]
    [InlineData(2026, false)]
    [InlineData(2027, true)]
    [InlineData(2028, true)]
    public void Season_FromEstablishmentYearNot365Days(int year, bool expected)
    {
        var profile = Profile();
        var result = SeasonalPhenology.Project(profile, 2026, year, Option.None<DateWindow>(), new()).ValueOrThrow();
        Assert.Equal(year - 2026 + 1, result.AgeSeason);
        Assert.Equal(expected, result.Periods.Fruiting is SeasonalPeriod.Known);
    }

    [Fact]
    public void MissingLaterPeriod_IsUnknownAndBiennialEndsAfterSecondSeason()
    {
        Assert.IsType<SeasonalPeriod.Unknown>(SeasonalPhenology.Project(Profile() with { FruitingLater = Option.None<AnnualDateWindow>() },
            2026, 2028, Option.None<DateWindow>(), new()).ValueOrThrow().Periods.Fruiting);
        Assert.IsType<SeasonalPeriod.NotExpected>(SeasonalPhenology.Project(Profile() with { Kind = LifeCycleKind.Biennial },
            2026, 2028, Option.None<DateWindow>(), new()).ValueOrThrow().Periods.Fruiting);
        Assert.True(SeasonalPhenology.Project(Profile(), 2026, 2025, Option.None<DateWindow>(), new()).IsFailure);
    }

    [Fact]
    public void LeapDay_IsUnknownWithoutFabricatingDateAndManualHasItsSource()
    {
        var profile = Profile() with { FruitingLater = Option.Some(AnnualDateWindow.Create(2, 29, 3, 1, false).ValueOrThrow()) };
        Assert.IsType<SeasonalPeriod.Unknown>(SeasonalPhenology.Project(profile, 2026, 2027, Option.None<DateWindow>(), new()).ValueOrThrow().Periods.Fruiting);
        var manual = DateWindow.Create(new(2027, 7, 1), new(2027, 9, 1)).ValueOrThrow();
        var result = SeasonalPhenology.Project(profile, 2026, 2027, Option.None<DateWindow>(), new() { Fruiting = Option.Some(manual) }).ValueOrThrow();
        Assert.Equal(GrowthPeriodOrigin.Manual, Assert.IsType<SeasonalPeriod.Known>(result.Periods.Fruiting).Period.Origin);
    }

    private static LifeCycleProfile Profile() => new() { Kind = LifeCycleKind.Perennial, FirstHarvestSeason = 2, FirstFloweringSeason = 2,
        FruitingLater = Option.Some(AnnualDateWindow.Create(7, 1, 9, 1, false).ValueOrThrow()) };
}
