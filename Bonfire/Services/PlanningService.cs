using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Bonfire.Application.Planning;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Planning.Regions;
using Bonfire.Services.Interfaces;

namespace Bonfire.Services;

internal sealed class PlanningService(
    IPlantingPlanReader reader,
    IPlanningContextReader contexts,
    IPlanningLibraryReader libraries,
    ICalculatePlannedBatch calculate,
    IManagePlantingPlans manage,
    ISavePlanningLibraries editLibraries,
    IPlanningBedReader beds,
    ICreateSeedShoppingList shopping) : IPlanningService
{
    public Task<Result<SeedShoppingListFromViewModel>> ReadShoppingListAsync(int planId, CancellationToken cancellationToken = default)
        => shopping.ExecuteAsync(planId, cancellationToken);
    public Task<Result<ImmutableArray<PlanningBedFromViewModel>>> ReadBedsAsync(
        CancellationToken cancellationToken = default) => beds.ReadAsync(cancellationToken);

    public Task<Result<ImmutableArray<PlantingPlanSummaryFromViewModel>>> ReadPlansAsync(
        CancellationToken cancellationToken = default) => reader.ReadAllAsync(cancellationToken);

    public Task<Result<Option<PlantingPlanDetailsFromViewModel>>> ReadPlanAsync(int id,
        CancellationToken cancellationToken = default)
        => SeasonalPlantingPlanId.Create(id).Match(key => reader.ReadAsync(key, cancellationToken),
            error => Task.FromResult(Result.Failure<Option<PlantingPlanDetailsFromViewModel>>(error)));

    public Task<Result<ImmutableArray<RegionFromViewModel>>> ReadRegionsAsync(bool includeArchived = false,
        CancellationToken cancellationToken = default)
        => libraries.ReadRegionsAsync(includeArchived, cancellationToken);

    public Task<Result<ImmutableArray<PlanningChoiceFromViewModel>>> ReadChoicesAsync(
        CancellationToken cancellationToken = default) => contexts.ReadChoicesAsync(cancellationToken);

    public Task<Result<PlanningContextFromViewModel>> ReadContextAsync(int regionId, int cultureId, int sortId,
        CancellationToken cancellationToken = default)
        => RegionId.Create(regionId).Bind(r => CultureId.Create(cultureId).Bind(c =>
                SortId.Create(sortId).Bind(s => Result.Success((Region: r, Culture: c, Sort: s)))))
            .Match(ids => contexts.ReadAsync(ids.Region, ids.Culture, ids.Sort, cancellationToken),
                error => Task.FromResult(Result.Failure<PlanningContextFromViewModel>(error)));

    public Task<Result<PlanningPreviewFromViewModel>> CalculateAsync(PlanningBatchInputFromViewModel input,
        CancellationToken cancellationToken = default) => calculate.ExecuteAsync(input, cancellationToken);

    public Task<Result<PlantingPlanDetailsFromViewModel>> CreatePlanAsync(string name, int year, int regionId,
        CancellationToken cancellationToken = default) => manage.CreateAsync(name, year, regionId, cancellationToken);

    public Task<Result<PlantingPlanDetailsFromViewModel>> SaveBatchAsync(SavePlannedBatchFromViewModel input,
        CancellationToken cancellationToken = default) => manage.SaveBatchAsync(input, cancellationToken);

    public Task<Result<PlantingPlanDetailsFromViewModel>> DeleteBatchAsync(int planId, int expectedVersion, int batchId,
        CancellationToken cancellationToken = default) =>
        manage.DeleteBatchAsync(planId, expectedVersion, batchId, cancellationToken);

    public Task<Result<int>> DeletePlanAsync(int planId, int expectedVersion,
        CancellationToken cancellationToken = default) =>
        manage.DeletePlanAsync(planId, expectedVersion, cancellationToken);

    public Task<Result<RegionFromViewModel>> SaveRegionAsync(SaveRegionFromViewModel input,
        CancellationToken cancellationToken = default) => editLibraries.SaveRegionAsync(input, cancellationToken);

    public Task<Result<CultureGrowingProfile>> SaveCultureProfileAsync(SaveCultureProfileFromViewModel input,
        CancellationToken cancellationToken = default) =>
        editLibraries.SaveCultureProfileAsync(input, cancellationToken);

    public Task<Result<SortPlanningSettings>> SaveSortSettingsAsync(SaveSortSettingsFromViewModel input,
        CancellationToken cancellationToken = default) => editLibraries.SaveSortSettingsAsync(input, cancellationToken);

    public Task<Result<RegionalPlantingWindow>> SaveRegionalWindowAsync(SaveRegionalWindowFromViewModel input,
        CancellationToken cancellationToken = default) =>
        editLibraries.SaveRegionalWindowAsync(input, cancellationToken);
}
