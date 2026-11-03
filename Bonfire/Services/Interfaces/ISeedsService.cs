using System.Collections.Immutable;
using System.Threading.Tasks;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using Bonfire.Models;

namespace Bonfire.Services.Interfaces;

public interface ISeedsService
{
    long InventoryVersion { get; }
    long OptionsVersion { get; }

    Task<Result<ImmutableArray<SeedReadFromViewModel>>> ReadSeedsAsync();
    Task<Result<AcquisitionResultFromViewModel>> AddOrUpdateSeedAsync(AddSeedRequest request);
    Task<Result<SeedEditResultFromViewModel>> SaveSeedEditAsync(EditSeedFromViewModel request);
    Task<Result<SeedEditorOptionsFromViewModel>> GetEditorOptionsAsync();
    Task<Result<SeedDeletionResultFromViewModel>> DeleteSeedWithReasonAsync(int seedId, string reason);
}