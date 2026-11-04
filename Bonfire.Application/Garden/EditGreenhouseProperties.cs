using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Application.Garden;

public sealed record EditGreenhousePropertiesFromViewModel(
    int Id,
    GreenhouseEditorValues Original,
    GreenhouseEditorValues Values,
    double PixelsPerMeter);

public sealed record GreenhousePropertiesResultFromViewModel(
    int Id,
    GreenhouseEditorValues Values,
    CanvasRectangle Canvas);

public interface IGreenhousePropertiesWriter
{
    Task<Result<GreenhousePropertiesResultFromViewModel>> ApplyAsync(GreenhousePropertiesEdit edit,
        CancellationToken cancellationToken);
}

public interface IEditGreenhouseProperties
{
    Task<Result<GreenhousePropertiesResultFromViewModel>> ExecuteAsync(EditGreenhousePropertiesFromViewModel request,
        CancellationToken cancellationToken = default);
}

public sealed class EditGreenhouseProperties(IGreenhousePropertiesWriter writer) : IEditGreenhouseProperties
{
    public Task<Result<GreenhousePropertiesResultFromViewModel>> ExecuteAsync(
        EditGreenhousePropertiesFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return GreenhousePropertiesEdit.Create(request.Id, request.Original, request.Values, request.PixelsPerMeter)
            .Match(
                edit => writer.ApplyAsync(edit, cancellationToken),
                error => Task.FromResult(Result.Failure<GreenhousePropertiesResultFromViewModel>(error)));
    }
}