using Bonfire.Application.Caching;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Bonfire.Application.Harvest;
using Bonfire.Domain.Functional;
using Bonfire.Services.Interfaces;

namespace Bonfire.Services;

internal sealed class HarvestService(
    IHarvestJournalReader reader,
    IHarvestSourceReader sources,
    IManageHarvestJournal manage,
    IReadModelCache cache) : IHarvestService
{
    private const ReadArea Scope = ReadArea.Harvest | ReadArea.Catalog | ReadArea.PlantingPlans | ReadArea.Perennials;
    public long ReadVersion => cache.Version(Scope);
    public void RefreshReadCache() => cache.Invalidate(Scope);

    public Task<Result<ImmutableArray<HarvestEntryFromViewModel>>> ReadAsync(
        CancellationToken cancellationToken = default) => cache.ReadAsync(ReadArea.Harvest, nameof(ReadAsync), reader.ReadAsync, cancellationToken);

    public Task<Result<ImmutableArray<HarvestSourceFromViewModel>>> ReadSourcesAsync(int seasonYear,
        CancellationToken cancellationToken = default) => cache.ReadAsync(ReadArea.PlantingPlans | ReadArea.Perennials | ReadArea.Catalog, (nameof(ReadSourcesAsync), seasonYear), token => sources.ReadSourcesAsync(seasonYear, token), cancellationToken);

    public Task<Result<HarvestEntryFromViewModel>> SaveAsync(SaveHarvestFromViewModel input,
        CancellationToken cancellationToken = default) => manage.SaveAsync(input, cancellationToken);

    public Task<Result<int>> DeleteAsync(int id, int expectedVersion, CancellationToken cancellationToken = default) =>
        manage.DeleteAsync(id, expectedVersion, cancellationToken);
}