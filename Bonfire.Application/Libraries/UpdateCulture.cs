using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Bonfire.Application.Libraries;

public sealed record UpdateCultureFromViewModel(int Id, string Name, string Class);

public sealed record CultureDetailsFromViewModel(int Id, string Name, string Class);

public interface ICultureWriter
{
    Task<Result<CultureDetailsFromViewModel>> SaveAsync(CultureDefinition culture, CancellationToken cancellationToken);
}

public interface IUpdateCulture
{
    Task<Result<CultureDetailsFromViewModel>> ExecuteAsync(UpdateCultureFromViewModel request,
        CancellationToken cancellationToken = default);
}

public sealed class UpdateCulture(ICultureWriter writer) : IUpdateCulture
{
    public async Task<Result<CultureDetailsFromViewModel>> ExecuteAsync(UpdateCultureFromViewModel request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return await CultureDefinition.Create(request.Id, request.Name, request.Class).Match(
            culture => writer.SaveAsync(culture, cancellationToken),
            error => Task.FromResult(Result.Failure<CultureDetailsFromViewModel>(error))).ConfigureAwait(false);
    }
}