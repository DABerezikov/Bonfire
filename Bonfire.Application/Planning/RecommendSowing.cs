using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;

namespace Bonfire.Application.Planning;

public sealed record SowingRecommendationRequestFromViewModel(
    GrowingMethod Method,
    EffectiveGrowingProfile Profile,
    PlanningChoices Choices,
    Option<EffectivePlanningExtras> Extras,
    LunarGroup Group,
    TimeZoneInfo TimeZone,
    int SearchLimitDays);

public sealed record SowingPreviewFromViewModel(PlantingSchedule Agronomy, SowingRecommendations Lunar);

public interface IRecommendSowing
{
    Result<SowingPreviewFromViewModel> Execute(SowingRecommendationRequestFromViewModel request,
        CancellationToken cancellationToken = default);
}

public sealed class RecommendSowing(ILunarTrendCalendar calendar) : IRecommendSowing
{
    public Result<SowingPreviewFromViewModel> Execute(SowingRecommendationRequestFromViewModel request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.TimeZone);
        cancellationToken.ThrowIfCancellationRequested();
        // Кэш только одного preview: ручной день внутри окна не вычисляется повторно.
        var days = new Dictionary<DateOnly, Result<LunarTrend>>();

        Result<LunarTrend> Read(DateOnly date)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (days.TryGetValue(date, out var cached)) return cached;
            var result = calendar.GetTrend(date, request.TimeZone);
            cancellationToken.ThrowIfCancellationRequested();
            days.Add(date, result);
            return result;
        }

        return PlantingSchedule.Calculate(request.Method, request.Profile, request.Choices, request.Extras)
            .Bind(agronomy =>
                SowingRecommendations.Calculate(request.Method, request.Profile, request.Choices, request.Extras,
                        request.Group, Read, request.SearchLimitDays)
                    .Bind(lunar => Result.Success(new SowingPreviewFromViewModel(agronomy, lunar))));
    }
}