using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Application.Garden;

public sealed record CreateGardenFromViewModel(int PlanId, string Name, double WidthMeters, double HeightMeters, double PixelsPerMeter);

public interface IGardenCreationWriter
{
    Task<Result<GardenPropertiesResultFromViewModel>> CreateAsync(GardenCreation creation, CancellationToken cancellationToken);
}

public interface ICreateGarden
{
    Task<Result<GardenPropertiesResultFromViewModel>> ExecuteAsync(CreateGardenFromViewModel request, CancellationToken cancellationToken = default);
}

public sealed class CreateGarden(IGardenCreationWriter writer) : ICreateGarden
{
    public Task<Result<GardenPropertiesResultFromViewModel>> ExecuteAsync(CreateGardenFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return GardenCreation.Create(request.PlanId, request.Name, request.WidthMeters, request.HeightMeters, request.PixelsPerMeter).Match(
            creation => writer.CreateAsync(creation, cancellationToken),
            error => Task.FromResult(Result.Failure<GardenPropertiesResultFromViewModel>(error)));
    }
}
