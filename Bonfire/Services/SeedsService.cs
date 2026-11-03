using Bonfire.Application.Caching;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using Bonfire.Models;
using Bonfire.Models.Mappers;
using Bonfire.Services.Interfaces;

namespace Bonfire.Services;

internal class SeedsService(
    IEditSeed editSeed,
    IAcquireSeeds acquireSeeds,
    IDeleteSeed deleteSeed,
    ISeedReader seedReader,
    ISeedEditorOptionsReader editorOptions,
    IReadModelCache cache) : ISeedsService
{
    public long InventoryVersion => cache.Version(ReadArea.Seeds | ReadArea.Catalog);
    public long OptionsVersion => cache.Version(ReadArea.Catalog | ReadArea.SortOptions);

    public Task<Result<ImmutableArray<SeedReadFromViewModel>>> ReadSeedsAsync() => cache.ReadAsync(ReadArea.Seeds | ReadArea.Catalog, nameof(ReadSeedsAsync), seedReader.ReadAsync);

    public Task<Result<SeedEditResultFromViewModel>> SaveSeedEditAsync(EditSeedFromViewModel request)
        => editSeed.ExecuteAsync(request);

    public Task<Result<SeedEditorOptionsFromViewModel>> GetEditorOptionsAsync() => cache.ReadAsync(ReadArea.Catalog | ReadArea.SortOptions, nameof(GetEditorOptionsAsync), editorOptions.ReadAsync);

    public Task<Result<AcquisitionResultFromViewModel>> AddOrUpdateSeedAsync(AddSeedRequest request)
        => SeedAcquisitionMapper.ToRequest(request).Match(
            acquisition => acquireSeeds.ExecuteAsync(acquisition),
            error => Task.FromResult(Result.Failure<AcquisitionResultFromViewModel>(error)));

    public Task<Result<SeedDeletionResultFromViewModel>> DeleteSeedWithReasonAsync(int seedId, string reason)
        => deleteSeed.ExecuteAsync(new DeleteSeedFromViewModel(seedId, reason));
}