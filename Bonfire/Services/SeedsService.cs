using System.Collections.Immutable;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using System.Threading.Tasks;
using Bonfire.Models;
using Bonfire.Models.Mappers;
using Bonfire.Services.Interfaces;

namespace Bonfire.Services;

internal class SeedsService(IEditSeed editSeed, IAcquireSeeds acquireSeeds, IDeleteSeed deleteSeed, ISeedReader seedReader, ISeedEditorOptionsReader editorOptions) : ISeedsService
{
    public Task<Result<ImmutableArray<SeedReadFromViewModel>>> ReadSeedsAsync() => seedReader.ReadAsync(default);

    public Task<Result<SeedEditResultFromViewModel>> SaveSeedEditAsync(EditSeedFromViewModel request)
        => editSeed.ExecuteAsync(request);

    public Task<Result<SeedEditorOptionsFromViewModel>> GetEditorOptionsAsync() => editorOptions.ReadAsync(default);

    public Task<Result<AcquisitionResultFromViewModel>> AddOrUpdateSeedAsync(AddSeedRequest request)
        => SeedAcquisitionMapper.ToRequest(request).Match(
            acquisition => acquireSeeds.ExecuteAsync(acquisition),
            error => Task.FromResult(Result.Failure<AcquisitionResultFromViewModel>(error)));

    public Task<Result<SeedDeletionResultFromViewModel>> DeleteSeedWithReasonAsync(int seedId, string reason)
        => deleteSeed.ExecuteAsync(new DeleteSeedFromViewModel(seedId, reason));

}
