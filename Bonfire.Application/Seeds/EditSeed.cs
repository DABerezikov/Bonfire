using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Bonfire.Application.Seeds;

public sealed record EditSeedFromViewModel(int SeedId, int SortId, int ProducerId, string SortName,
    SeedPackageValues Package, SortCharacteristicsValues Characteristics);
public sealed record SeedEditResultFromViewModel(int SeedId, int SortId);

public interface ISeedEditWriter
{
    Task<Result<SeedEditResultFromViewModel>> SaveAsync(SeedEdit edit, CancellationToken cancellationToken);
}

public interface IEditSeed
{
    Task<Result<SeedEditResultFromViewModel>> ExecuteAsync(EditSeedFromViewModel request, CancellationToken cancellationToken = default);
}

public sealed class EditSeed(ISeedEditWriter writer) : IEditSeed
{
    public async Task<Result<SeedEditResultFromViewModel>> ExecuteAsync(EditSeedFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return await SeedEdit.Create(request.SeedId, request.SortId, request.ProducerId,
            request.SortName, request.Package, request.Characteristics).Match(
                edit => writer.SaveAsync(edit, cancellationToken),
                error => Task.FromResult(Result.Failure<SeedEditResultFromViewModel>(error))).ConfigureAwait(false);
    }
}
