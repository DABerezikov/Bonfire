using System.Collections.Immutable;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning.GrowingProfiles;

namespace Bonfire.Domain.Planning;

public enum LunarUnavailableReason { UnspecifiedGroup, CalendarFailure, AlternativeCalculationFailure }

public sealed record ManualSowingAssessment(DateOnly Date, LunarTrend Trend, bool MatchesPreference,
    bool InsideSowingWindow, bool InsideResilientWindow);

public sealed record SowingCandidate(DateOnly Date, LunarTrend Trend, int ShiftDays, bool InsideResilientWindow,
    PlantingSchedule Preview, Option<CandidateReadiness> Readiness);

public abstract record SowingRecommendations
{
    private SowingRecommendations() { }

    public sealed record Available(ImmutableArray<SowingCandidate> Candidates, bool HasResilientCandidates, bool ResilientPreferenceMissed,
        Option<ManualSowingAssessment> ManualChoice) : SowingRecommendations;

    public sealed record Conflict(DateWindow OriginalWindow, Option<SowingCandidate> Earlier,
        Option<SowingCandidate> Later, int SearchLimitDays, Option<ManualSowingAssessment> ManualChoice) : SowingRecommendations;

    public sealed record Unavailable(LunarUnavailableReason Reason, string Message) : SowingRecommendations;

    /// <summary>Функция чтения фаз передаётся снаружи; ядро не знает астрономического поставщика.</summary>
    public static Result<SowingRecommendations> Calculate(GrowingMethod method, EffectiveGrowingProfile profile,
        PlanningChoices choices, Option<EffectivePlanningExtras> extras, LunarGroup group,
        Func<DateOnly, Result<LunarTrend>> calendar, int searchLimitDays)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        if (searchLimitDays < 0)
            return Result.Failure<SowingRecommendations>("Предел поиска сдвига не может быть отрицательным");
        if (!Enum.IsDefined(group))
            return Result.Failure<SowingRecommendations>("Неизвестная лунная группа культуры");
        return PlantingSchedule.Calculate(method, profile, choices, extras).Bind(schedule =>
            LunarSowingPolicy.PreferredTrend(group).Match(preferred =>
                Recommend(schedule, method, profile, choices, extras, preferred, calendar, searchLimitDays),
                error => Result.Success<SowingRecommendations>(new Unavailable(LunarUnavailableReason.UnspecifiedGroup, error))));
    }

    private static Result<SowingRecommendations> Recommend(PlantingSchedule schedule, GrowingMethod method,
        EffectiveGrowingProfile profile, PlanningChoices choices, Option<EffectivePlanningExtras> extras,
        LunarTrend preferred, Func<DateOnly, Result<LunarTrend>> calendar, int limit)
    {
        Result<LunarTrend> Read(DateOnly date) => calendar(date).Bind(trend => Enum.IsDefined(trend)
            ? Result.Success(trend) : Result.Failure<LunarTrend>("Календарь вернул неизвестную фазу"));
        Option<ManualSowingAssessment> manual = Option.None<ManualSowingAssessment>();
        if (choices.SowingDate is Option<DateOnly>.Some selected)
        {
            var trend = Read(selected.Value);
            if (trend.IsFailure) return CalendarFailure(trend, selected.Value);
            manual = Option.Some(new ManualSowingAssessment(selected.Value, trend.ValueOrThrow(), trend.ValueOrThrow() == preferred,
                schedule.Sowing.Contains(selected.Value), schedule.ResilientSowing.Match(x => x.Contains(selected.Value), () => false)));
        }

        var candidates = ImmutableArray.CreateBuilder<SowingCandidate>();
        for (var day = schedule.Sowing.Start.DayNumber; day <= schedule.Sowing.End.DayNumber; day++)
        {
            var date = DateOnly.FromDayNumber(day);
            var trend = Read(date);
            if (trend.IsFailure) return CalendarFailure(trend, date);
            if (trend.ValueOrThrow() != preferred) continue;
            var candidate = MakeCandidate(date, preferred, 0, schedule, method, profile, choices, extras);
            if (candidate.IsFailure)
                return candidate.Match(_ => throw new InvalidOperationException(), Result.Failure<SowingRecommendations>);
            candidates.Add(candidate.ValueOrThrow());
        }
        if (candidates.Count > 0)
            return Result.Success<SowingRecommendations>(new Available(
                candidates.OrderByDescending(x => x.InsideResilientWindow).ThenBy(x => x.Date).ToImmutableArray(),
                candidates.Any(x => x.InsideResilientWindow),
                schedule.ResilientSowing is Option<DateWindow>.Some && !candidates.Any(x => x.InsideResilientWindow), manual));

        // Границы поиска ограничиваются календарём, а не переносом даты на другой год.
        var earlier = Find(schedule.Sowing.Start, -1);
        return earlier.Bind(e => Find(schedule.Sowing.End, 1)
                .Bind(l => Result.Success<SowingRecommendations>(new Conflict(schedule.Sowing, e, l, limit, manual))))
            .Match(Result.Success, error => Result.Success<SowingRecommendations>(new Unavailable(LunarUnavailableReason.AlternativeCalculationFailure, error)));

        Result<Option<SowingCandidate>> Find(DateOnly boundary, int direction)
        {
            var maxDistance = direction < 0 ? boundary.DayNumber : DateOnly.MaxValue.DayNumber - boundary.DayNumber;
            for (var distance = 1; distance <= Math.Min(limit, maxDistance); distance++)
            {
                var date = DateOnly.FromDayNumber(boundary.DayNumber + direction * distance);
                var trend = Read(date);
                if (trend.IsFailure) return trend.Bind(_ => Result.Success(Option.None<SowingCandidate>()));
                if (trend.ValueOrThrow() == preferred)
                    return MakeCandidate(date, preferred, direction * distance, schedule, method, profile, choices, extras)
                        .Bind(candidate => Result.Success(Option.Some(candidate)));
            }
            return Result.Success(Option.None<SowingCandidate>());
        }
    }

    private static Result<SowingRecommendations> CalendarFailure(Result<LunarTrend> trend, DateOnly date)
        => trend.Match(_ => throw new InvalidOperationException(), error => Result.Success<SowingRecommendations>(
            new Unavailable(LunarUnavailableReason.CalendarFailure, $"Нет лунной рекомендации на {date:dd.MM.yyyy}: {error}")));

    private static Result<SowingCandidate> MakeCandidate(DateOnly date, LunarTrend trend, int shift,
        PlantingSchedule original, GrowingMethod method, EffectiveGrowingProfile profile, PlanningChoices choices,
        Option<EffectivePlanningExtras> extras)
        => PlantingSchedule.Calculate(method, profile, choices with { SowingDate = Option.Some(date) }, extras)
            .Bind(preview => (method is GrowingMethod.Transplant transplant
                ? profile.SeedlingAge.Match(age => PlantingSchedule.EvaluateCandidate(date, transplant.TransplantWindow,
                    profile.Germination.Value, age.Value).Bind(x => Result.Success(Option.Some(x))),
                    () => Result.Failure<Option<CandidateReadiness>>("Не задан возраст рассады"))
                : Result.Success(Option.None<CandidateReadiness>()))
                .Bind(readiness => Result.Success(new SowingCandidate(date, trend, shift,
                    original.ResilientSowing.Match(window => window.Contains(date), () => false), preview, readiness))));
}
