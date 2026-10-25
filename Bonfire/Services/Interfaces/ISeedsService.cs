using System.Collections.Immutable;
using Bonfire.Models;
﻿using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using System.Threading.Tasks;

namespace Bonfire.Services.Interfaces;

public interface ISeedsService
{
    Task<Result<ImmutableArray<SeedReadFromViewModel>>> ReadSeedsAsync();
    Task<Result<AcquisitionResultFromViewModel>> AddOrUpdateSeedAsync(AddSeedRequest request);
    Task<Result<SeedEditResultFromViewModel>> SaveSeedEditAsync(EditSeedFromViewModel request);
    Task<Result<SeedEditorOptionsFromViewModel>> GetEditorOptionsAsync();
    Task<Result<SeedDeletionResultFromViewModel>> DeleteSeedWithReasonAsync(int seedId, string reason);
}
