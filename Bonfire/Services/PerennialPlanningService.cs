using Bonfire.Application.Caching;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Services.Interfaces;

namespace Bonfire.Services;

internal sealed class PerennialPlanningService(IPerennialPlanningReader reader, IManagePerennialPlanning manage,
    IReadModelCache cache)
    : IPerennialPlanningService
{
    private const ReadArea Scope = ReadArea.Perennials | ReadArea.PlantingPlans;
    public long ReadVersion => cache.Version(Scope);
    public void RefreshReadCache() => cache.Invalidate(Scope);

    public Task<Result<ImmutableArray<PerennialPlantingFromViewModel>>> ReadAsync(
        CancellationToken cancellationToken = default) => cache.ReadAsync(ReadArea.Perennials, nameof(ReadAsync), reader.ReadAsync, cancellationToken);

    public Task<Result<ImmutableArray<PerennialSeasonFromViewModel>>> ReadSeasonsAsync(int planId,
        CancellationToken cancellationToken = default) => cache.ReadAsync(ReadArea.Perennials | ReadArea.PlantingPlans, (nameof(ReadSeasonsAsync), planId), token => reader.ReadSeasonsAsync(planId, token), cancellationToken);

    public Task<Result<PerennialPlantingFromViewModel>> SaveAsync(Option<int> id, int version,
        PerennialPlantingDefinition definition, CancellationToken cancellationToken = default) =>
        manage.SaveAsync(id, version, definition, cancellationToken);

    public Task<Result<int>> DeleteAsync(int id, int version, CancellationToken cancellationToken = default) =>
        manage.DeleteAsync(id, version, cancellationToken);

    public Task<Result<PerennialSeasonPreview>> CalculateAsync(int planId, int plantingId, GrowthPeriodOverrides manual,
        CancellationToken cancellationToken = default) =>
        manage.CalculateAsync(planId, plantingId, manual, cancellationToken);

    public Task<Result<SavedPerennialSeason>> SaveSeasonAsync(PerennialSeasonPreview preview,
        CancellationToken cancellationToken = default) => manage.SaveSeasonAsync(preview, cancellationToken);

    public Task<Result<int>> DeleteSeasonAsync(int planId, int expectedPlanVersion, int plantingId,
        CancellationToken cancellationToken = default) =>
        manage.DeleteSeasonAsync(planId, expectedPlanVersion, plantingId, cancellationToken);
}