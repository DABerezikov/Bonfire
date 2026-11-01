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
    IManageHarvestJournal manage) : IHarvestService
{
    public Task<Result<ImmutableArray<HarvestEntryFromViewModel>>> ReadAsync(
        CancellationToken cancellationToken = default) => reader.ReadAsync(cancellationToken);

    public Task<Result<ImmutableArray<HarvestSourceFromViewModel>>> ReadSourcesAsync(int seasonYear,
        CancellationToken cancellationToken = default) => sources.ReadSourcesAsync(seasonYear, cancellationToken);

    public Task<Result<HarvestEntryFromViewModel>> SaveAsync(SaveHarvestFromViewModel input,
        CancellationToken cancellationToken = default) => manage.SaveAsync(input, cancellationToken);

    public Task<Result<int>> DeleteAsync(int id, int expectedVersion, CancellationToken cancellationToken = default) =>
        manage.DeleteAsync(id, expectedVersion, cancellationToken);
}