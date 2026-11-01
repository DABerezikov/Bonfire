using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Bonfire.Application.Planning;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Planning.Regions;

namespace Bonfire.Services.Interfaces;

public interface IPlanningService
{
    Task<Result<ImmutableArray<PlanningBedFromViewModel>>> ReadBedsAsync(CancellationToken cancellationToken = default);
    Task<Result<ImmutableArray<PlantingPlanSummaryFromViewModel>>> ReadPlansAsync(CancellationToken cancellationToken = default);
    Task<Result<Option<PlantingPlanDetailsFromViewModel>>> ReadPlanAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<ImmutableArray<RegionFromViewModel>>> ReadRegionsAsync(bool includeArchived = false, CancellationToken cancellationToken = default);
    Task<Result<ImmutableArray<PlanningChoiceFromViewModel>>> ReadChoicesAsync(CancellationToken cancellationToken = default);
    Task<Result<PlanningContextFromViewModel>> ReadContextAsync(int regionId, int cultureId, int sortId, CancellationToken cancellationToken = default);
    Task<Result<PlanningPreviewFromViewModel>> CalculateAsync(PlanningBatchInputFromViewModel input, CancellationToken cancellationToken = default);
    Task<Result<PlantingPlanDetailsFromViewModel>> CreatePlanAsync(string name, int year, int regionId, CancellationToken cancellationToken = default);
    Task<Result<PlantingPlanDetailsFromViewModel>> SaveBatchAsync(SavePlannedBatchFromViewModel input, CancellationToken cancellationToken = default);
    Task<Result<PlantingPlanDetailsFromViewModel>> DeleteBatchAsync(int planId, int expectedVersion, int batchId, CancellationToken cancellationToken = default);
    Task<Result<int>> DeletePlanAsync(int planId, int expectedVersion, CancellationToken cancellationToken = default);
    Task<Result<RegionFromViewModel>> SaveRegionAsync(SaveRegionFromViewModel input, CancellationToken cancellationToken = default);
    Task<Result<CultureGrowingProfile>> SaveCultureProfileAsync(SaveCultureProfileFromViewModel input, CancellationToken cancellationToken = default);
    Task<Result<SortPlanningSettings>> SaveSortSettingsAsync(SaveSortSettingsFromViewModel input, CancellationToken cancellationToken = default);
    Task<Result<RegionalPlantingWindow>> SaveRegionalWindowAsync(SaveRegionalWindowFromViewModel input, CancellationToken cancellationToken = default);
}
