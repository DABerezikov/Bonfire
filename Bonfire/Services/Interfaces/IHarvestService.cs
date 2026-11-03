using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Bonfire.Application.Harvest;
using Bonfire.Domain.Functional;

namespace Bonfire.Services.Interfaces;

public interface IHarvestService
{
    long ReadVersion { get; }
    void RefreshReadCache();

    Task<Result<ImmutableArray<HarvestEntryFromViewModel>>> ReadAsync(CancellationToken cancellationToken = default);

    Task<Result<ImmutableArray<HarvestSourceFromViewModel>>> ReadSourcesAsync(int seasonYear,
        CancellationToken cancellationToken = default);

    Task<Result<HarvestEntryFromViewModel>> SaveAsync(SaveHarvestFromViewModel input,
        CancellationToken cancellationToken = default);

    Task<Result<int>> DeleteAsync(int id, int expectedVersion, CancellationToken cancellationToken = default);
}