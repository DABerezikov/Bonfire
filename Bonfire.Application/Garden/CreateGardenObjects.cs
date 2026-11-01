using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Application.Garden;

public sealed record CreateGardenElementFromViewModel(
    int PlotId,
    string Name,
    GardenElementKind Kind,
    double X,
    double Y,
    double Width,
    double Height);

public sealed record CreateGreenhouseFromViewModel(
    int PlotId,
    string Name,
    double WidthMeters,
    double HeightMeters,
    double Scale,
    double X,
    double Y);

public sealed record ElementCreationResultFromViewModel(
    ElementPropertiesResultFromViewModel Properties,
    string ElementType,
    CanvasRectangle Container);

public sealed record GreenhouseCreationResultFromViewModel(
    GreenhousePropertiesResultFromViewModel Properties,
    CanvasRectangle Rectangle,
    CanvasRectangle Container);

public interface IGardenObjectCreationWriter
{
    Task<Result<ElementCreationResultFromViewModel>> CreateElementAsync(GardenObjectCreation.Element creation,
        CancellationToken cancellationToken);

    Task<Result<GreenhouseCreationResultFromViewModel>> CreateGreenhouseAsync(GardenObjectCreation.Greenhouse creation,
        CancellationToken cancellationToken);
}

public interface ICreateGardenObjects
{
    Task<Result<ElementCreationResultFromViewModel>> CreateElementAsync(CreateGardenElementFromViewModel request,
        CancellationToken cancellationToken = default);

    Task<Result<GreenhouseCreationResultFromViewModel>> CreateGreenhouseAsync(CreateGreenhouseFromViewModel request,
        CancellationToken cancellationToken = default);
}

public sealed class CreateGardenObjects(IGardenObjectCreationWriter writer) : ICreateGardenObjects
{
    public Task<Result<ElementCreationResultFromViewModel>> CreateElementAsync(CreateGardenElementFromViewModel request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return GardenObjectCreation.CreateElement(request.PlotId, request.Name, request.Kind, request.X, request.Y,
            request.Width, request.Height).Match(
            creation => writer.CreateElementAsync(creation, cancellationToken),
            error => Task.FromResult(Result.Failure<ElementCreationResultFromViewModel>(error)));
    }

    public Task<Result<GreenhouseCreationResultFromViewModel>> CreateGreenhouseAsync(
        CreateGreenhouseFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return GardenObjectCreation.CreateGreenhouse(request.PlotId, request.Name, request.WidthMeters,
            request.HeightMeters, request.Scale, request.X, request.Y).Match(
            creation => writer.CreateGreenhouseAsync(creation, cancellationToken),
            error => Task.FromResult(Result.Failure<GreenhouseCreationResultFromViewModel>(error)));
    }
}