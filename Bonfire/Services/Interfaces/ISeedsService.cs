using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bonfire.Models;
using BonfireDB.Entities;

namespace Bonfire.Services.Interfaces;

public interface ISeedsService
{
    Task<IReadOnlyList<Seed>> GetAllSeedsAsync();
    Task<Seed?> GetSeedAsync(int id);
    Task<(Seed seed, bool isNew)> AddOrUpdateSeedAsync(AddSeedRequest request, IReadOnlyList<Seed> existingSeeds);
    Task<Seed> UpdateSeed(Seed seed);
    Task<Result<SeedEditResultFromViewModel>> SaveSeedEditAsync(EditSeedFromViewModel request);
    Task<SeedEditorOptions> GetEditorOptionsAsync();
    Task<Seed> DeleteSeed(Seed seed);
    Task<Seed> DeleteSeedWithReasonAsync(int seedId, string reason);
    Task ReturnSeedsFromSeedling(int seedId, double quantity, double? weight);
    Task<PlantSort> UpdateSort(PlantSort seed);
    Task<PlantCulture> UpdateCulture(PlantCulture culture);
    Task<Producer> UpdateProducer(Producer producer);
}
