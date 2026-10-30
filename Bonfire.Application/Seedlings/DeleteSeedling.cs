using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;

namespace Bonfire.Application.Seedlings;

public sealed record DeleteSeedlingFromViewModel(int SeedlingId, SeedlingRemoval Removal);
public sealed record ReturnedSeedStockFromViewModel(int SeedId, double Pieces, Option<double> Grams);
public sealed record SeedlingDeletionResultFromViewModel(int SeedlingId, Option<ReturnedSeedStockFromViewModel> ReturnedStock);

public interface ISeedlingDeletionWriter
{
    Task<Result<SeedlingDeletionResultFromViewModel>> DeleteAsync(SeedlingId id, SeedlingRemoval removal, CancellationToken cancellationToken);
}

public interface IDeleteSeedling
{
    Task<Result<SeedlingDeletionResultFromViewModel>> ExecuteAsync(DeleteSeedlingFromViewModel request, CancellationToken cancellationToken = default);
}

public sealed class DeleteSeedling(ISeedlingDeletionWriter writer) : IDeleteSeedling
{
    public Task<Result<SeedlingDeletionResultFromViewModel>> ExecuteAsync(DeleteSeedlingFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Removal);
        cancellationToken.ThrowIfCancellationRequested();
        return SeedlingId.Create(request.SeedlingId).Match(
            id => writer.DeleteAsync(id, request.Removal, cancellationToken),
            error => Task.FromResult(Result.Failure<SeedlingDeletionResultFromViewModel>(error)));
    }
}
