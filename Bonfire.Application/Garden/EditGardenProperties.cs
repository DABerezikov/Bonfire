using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Application.Garden;

public sealed record EditGardenPropertiesFromViewModel(int Id, GardenEditorValues Original, GardenEditorValues Values, double PixelsPerMeter);
public sealed record GardenPropertiesResultFromViewModel(int Id, GardenEditorValues Values, CanvasRectangle Canvas);

public interface IGardenPropertiesWriter
{
    Task<Result<GardenPropertiesResultFromViewModel>> ApplyAsync(GardenPropertiesEdit edit, CancellationToken cancellationToken);
}

public interface IEditGardenProperties
{
    Task<Result<GardenPropertiesResultFromViewModel>> ExecuteAsync(EditGardenPropertiesFromViewModel request, CancellationToken cancellationToken = default);
}

public sealed class EditGardenProperties(IGardenPropertiesWriter writer) : IEditGardenProperties
{
    public Task<Result<GardenPropertiesResultFromViewModel>> ExecuteAsync(EditGardenPropertiesFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return GardenPropertiesEdit.Create(request.Id, request.Original, request.Values, request.PixelsPerMeter).Match(
            edit => writer.ApplyAsync(edit, cancellationToken),
            error => Task.FromResult(Result.Failure<GardenPropertiesResultFromViewModel>(error)));
    }
}
