using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Domain.Seeds;

namespace Bonfire.Application.Garden;

public sealed record PlantInSpotFromViewModel(
    int SpotId,
    PlantingSource Source,
    double Amount,
    DateTime Date,
    string Label,
    Option<string> Place);

public sealed record PlantingResultFromViewModel(
    int SpotId,
    int SeedlingInfoId,
    SeedAmount Consumed,
    SeedAmount Remaining);

public interface IPlantingWriter
{
    Task<Result<PlantingResultFromViewModel>> ApplyAsync(SpotPlanting planting, string lunarPhase,
        CancellationToken cancellationToken);
}

public interface IPlantInSpot
{
    Task<Result<PlantingResultFromViewModel>> ExecuteAsync(PlantInSpotFromViewModel request,
        CancellationToken cancellationToken = default);
}

public sealed class PlantInSpot(IPlantingWriter writer, ILunarCalendar calendar) : IPlantInSpot
{
    public Task<Result<PlantingResultFromViewModel>> ExecuteAsync(PlantInSpotFromViewModel request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return SpotPlanting.Create(request.SpotId, request.Source, request.Amount, request.Date, request.Label,
            request.Place).Match(
            planting => writer.ApplyAsync(planting, calendar.GetPhase(planting.Date), cancellationToken),
            error => Task.FromResult(Result.Failure<PlantingResultFromViewModel>(error)));
    }
}