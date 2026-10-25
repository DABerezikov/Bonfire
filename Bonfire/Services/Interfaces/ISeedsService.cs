using System.Collections.Immutable;
﻿using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bonfire.Models;
using BonfireDB.Entities;

namespace Bonfire.Services.Interfaces;

public interface ISeedsService
{
    Task<Result<ImmutableArray<SeedReadFromViewModel>>> ReadSeedsAsync();
    Task<IReadOnlyList<Seed>> GetAllSeedsAsync();
    Task<Seed?> GetSeedAsync(int id);
    Task<Result<AcquisitionResultFromViewModel>> AddOrUpdateSeedAsync(AddSeedRequest request);
    Task<Seed> UpdateSeed(Seed seed);
    Task<Result<SeedEditResultFromViewModel>> SaveSeedEditAsync(EditSeedFromViewModel request);
    Task<Result<SeedEditorOptionsFromViewModel>> GetEditorOptionsAsync();
    Task<Seed> DeleteSeed(Seed seed);
    Task<Result<SeedDeletionResultFromViewModel>> DeleteSeedWithReasonAsync(int seedId, string reason);
    Task ReturnSeedsFromSeedling(int seedId, double quantity, double? weight);
    Task<PlantSort> UpdateSort(PlantSort seed);
    Task<PlantCulture> UpdateCulture(PlantCulture culture);
    Task<Producer> UpdateProducer(Producer producer);
}
