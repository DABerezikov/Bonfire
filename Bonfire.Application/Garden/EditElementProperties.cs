using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Application.Garden;

public sealed record EditElementPropertiesFromViewModel(int Id, ElementEditorValues Original, ElementEditorValues Values, double PixelsPerMeter);
public sealed record ElementPropertiesResultFromViewModel(int Id, ElementDetailsValues Details, CanvasRectangle Rectangle, double AreaSquareMeters);

public interface IElementPropertiesWriter
{
    Task<Result<ElementPropertiesResultFromViewModel>> ApplyAsync(ElementPropertiesEdit edit, CancellationToken cancellationToken);
}

public interface IEditElementProperties
{
    Task<Result<ElementPropertiesResultFromViewModel>> ExecuteAsync(EditElementPropertiesFromViewModel request, CancellationToken cancellationToken = default);
}

public sealed class EditElementProperties(IElementPropertiesWriter writer) : IEditElementProperties
{
    public Task<Result<ElementPropertiesResultFromViewModel>> ExecuteAsync(EditElementPropertiesFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return ElementPropertiesEdit.Create(request.Id, request.Original, request.Values, request.PixelsPerMeter).Match(
            edit => writer.ApplyAsync(edit, cancellationToken),
            error => Task.FromResult(Result.Failure<ElementPropertiesResultFromViewModel>(error)));
    }
}
