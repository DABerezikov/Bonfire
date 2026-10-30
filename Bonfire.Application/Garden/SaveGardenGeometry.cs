using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Application.Garden;

public sealed record SaveGardenGeometryFromViewModel(int EntityId, GeometryTarget Target, double X, double Y, double Width, double Height);
public sealed record GeometryResultFromViewModel(int Id, CanvasRectangle Rectangle);

public interface IGardenGeometryWriter
{
    Task<Result<GeometryResultFromViewModel>> ApplyAsync(GardenGeometryEdit edit, CancellationToken cancellationToken);
}

public interface ISaveGardenGeometry
{
    Task<Result<GeometryResultFromViewModel>> ExecuteAsync(SaveGardenGeometryFromViewModel request, CancellationToken cancellationToken = default);
}

public sealed class SaveGardenGeometry(IGardenGeometryWriter writer) : ISaveGardenGeometry
{
    public Task<Result<GeometryResultFromViewModel>> ExecuteAsync(SaveGardenGeometryFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return GardenGeometryEdit.Create(request.EntityId, request.Target, request.X, request.Y, request.Width, request.Height).Match(
            edit => writer.ApplyAsync(edit, cancellationToken),
            error => Task.FromResult(Result.Failure<GeometryResultFromViewModel>(error)));
    }
}
