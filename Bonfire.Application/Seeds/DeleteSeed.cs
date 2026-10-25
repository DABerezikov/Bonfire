using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Bonfire.Application.Seeds;

public sealed record DeleteSeedFromViewModel(int SeedId, string Reason);
public sealed record SeedDeletionResultFromViewModel(int SeedId);

public interface ISeedDeletionWriter
{
    Task<Result<SeedDeletionResultFromViewModel>> DeleteAsync(SeedDeletion deletion, CancellationToken cancellationToken);
}

public interface IDeleteSeed
{
    Task<Result<SeedDeletionResultFromViewModel>> ExecuteAsync(DeleteSeedFromViewModel request, CancellationToken cancellationToken = default);
}

public sealed class DeleteSeed(ISeedDeletionWriter writer) : IDeleteSeed
{
    public async Task<Result<SeedDeletionResultFromViewModel>> ExecuteAsync(DeleteSeedFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return await SeedDeletion.Create(request.SeedId, request.Reason).Match(
            deletion => writer.DeleteAsync(deletion, cancellationToken),
            error => Task.FromResult(Result.Failure<SeedDeletionResultFromViewModel>(error))).ConfigureAwait(false);
    }
}
