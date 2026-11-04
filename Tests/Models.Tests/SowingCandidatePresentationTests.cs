using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Models.Planning;

namespace Models.Tests;

public class SowingCandidatePresentationTests
{
    [Fact]
    public void WideTransplantWindow_DisplaysCompatibleDatesAndAgeInsteadOfNegativeAge()
    {
        var item = Item(new DateOnly(2027, 1, 16), Window(2026, 11, 1, 2027, 3, 31));
        Assert.Equal(new AgeBounds(-91, 67), item.Candidate.Readiness.Match(x => x.AgeAtTarget, () => throw new Exception()));
        Assert.Contains("23.01.2027 – 31.01.2027", item.Details);
        Assert.Contains("26.01.2027 – 09.02.2027", item.Details);
        Assert.Contains("3–9 дн.", item.Details);
        Assert.DoesNotContain("-91", item.Details);
        Assert.Contains("ранних и поздних всходах", item.Label);
    }

    [Fact]
    public void SowingAfterTarget_ReportsConflictInsteadOfNegativeBiologicalAge()
    {
        var item = Item(new DateOnly(2027, 4, 1), Window(2027, 3, 1, 2027, 3, 31));
        Assert.Contains("Конфликт", item.Details);
        Assert.DoesNotContain("Возраст при высадке", item.Details);
        Assert.Contains("Готовность к высадке", item.Details);
    }

    [Fact]
    public void ShortCompatibleWindow_LimitsAgeToPossibleDaysWithinThatWindow()
    {
        var item = Item(new DateOnly(2027, 1, 16), Window(2027, 1, 27, 2027, 1, 29));
        Assert.Contains("Высадка в заданном периоде: 27.01.2027 – 29.01.2027", item.Details);
        Assert.Contains("Возраст при высадке: 3–6 дн.", item.Details);
        Assert.DoesNotContain("Конфликт", item.Details);
    }

    private static SowingCandidateItem Item(DateOnly sowing, DateWindow target)
    {
        var germination = DayRange.Create(7, 15).ValueOrThrow();
        var age = DayRange.Create(3, 9).ValueOrThrow();
        var profile = new EffectiveGrowingProfile(new(germination, ParameterSource.Batch),
            Option.Some(new SourcedValue<DayRange>(age, ParameterSource.Batch)), Option.None<SourcedValue<DayRange>>());
        var preview = PlantingSchedule.Calculate(new GrowingMethod.Transplant(target), profile,
            PlanningChoices.Empty with { SowingDate = Option.Some(sowing) }, Option.None<EffectivePlanningExtras>()).ValueOrThrow();
        return new(new(sowing, LunarTrend.Growing, 0, preview.ResilientSowing.Match(x => x.Contains(sowing), () => false),
            preview, Option.Some(PlantingSchedule.EvaluateCandidate(sowing, target, germination, age).ValueOrThrow())));
    }

    private static DateWindow Window(int y, int m, int d, int ey, int em, int ed)
        => DateWindow.Create(new(y, m, d), new(ey, em, ed)).ValueOrThrow();
}
