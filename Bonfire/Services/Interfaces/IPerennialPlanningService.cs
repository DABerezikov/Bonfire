using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
namespace Bonfire.Services.Interfaces;
public interface IPerennialPlanningService
{
    Task<Result<ImmutableArray<PerennialPlantingFromViewModel>>> ReadAsync(CancellationToken cancellationToken = default);
    Task<Result<ImmutableArray<PerennialSeasonFromViewModel>>> ReadSeasonsAsync(int planId, CancellationToken cancellationToken = default);
    Task<Result<PerennialPlantingFromViewModel>> SaveAsync(Option<int> id, int version, PerennialPlantingDefinition definition, CancellationToken cancellationToken = default);
    Task<Result<int>> DeleteAsync(int id, int version, CancellationToken cancellationToken = default);
    Task<Result<PerennialSeasonPreview>> CalculateAsync(int planId, int plantingId, GrowthPeriodOverrides manual, CancellationToken cancellationToken = default);
    Task<Result<SavedPerennialSeason>> SaveSeasonAsync(PerennialSeasonPreview preview, CancellationToken cancellationToken = default);
    Task<Result<int>> DeleteSeasonAsync(int planId, int expectedPlanVersion, int plantingId, CancellationToken cancellationToken = default);
}
