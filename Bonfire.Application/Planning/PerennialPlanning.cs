using System.Collections.Immutable;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;

namespace Bonfire.Application.Planning;

public sealed record PerennialPlantingFromViewModel(int Id, int Version, PerennialPlantingDefinition Definition,
    Option<int> CurrentCultureId, Option<int> CurrentSortId, Option<int> CurrentRegionId);
public sealed record PerennialSeasonSnapshot(PerennialPlantingDefinition Definition, GrowthPeriodOverrides Manual,
    SeasonalPhenology Projection, DateTimeOffset CalculatedAt, int CalculationVersion = 1)
{
    public Result<PerennialSeasonSnapshot> Validate() => CalculationVersion == 1
        ? SeasonalPhenology.Project(Definition.Profile.Value, Definition.EstablishmentYear, Projection.SeasonYear, Definition.FirstEmergence, Manual)
            .Bind(expected => expected == Projection ? Result.Success(this) : Result.Failure<PerennialSeasonSnapshot>("Прогноз не соответствует снимку входных данных"))
        : Result.Failure<PerennialSeasonSnapshot>("Неподдерживаемая версия прогноза периодов");
}
public sealed record PerennialSeasonFromViewModel(int Id, int Version, int PlanId, int PlantingId, PerennialSeasonSnapshot Snapshot);
public sealed record PerennialSeasonPreview(int PlanId, int PlanVersion, int PlantingId, int PlantingVersion, PerennialSeasonSnapshot Snapshot);
public sealed record SavedPerennialSeason(PerennialSeasonFromViewModel Season, int PlanVersion);
public sealed record PerennialWrite(Option<PerennialPlantingId> Id, int ExpectedVersion, PerennialPlantingDefinition Definition);
public interface IPerennialPlanningReader
{
    Task<Result<ImmutableArray<PerennialPlantingFromViewModel>>> ReadAsync(CancellationToken cancellationToken);
    Task<Result<ImmutableArray<PerennialSeasonFromViewModel>>> ReadSeasonsAsync(int planId, CancellationToken cancellationToken);
}
public interface IPerennialPlanningWriter
{
    Task<Result<PerennialPlantingFromViewModel>> SaveAsync(PerennialWrite request, CancellationToken cancellationToken);
    Task<Result<int>> DeleteAsync(PerennialPlantingId id, int expectedVersion, CancellationToken cancellationToken);
    Task<Result<SavedPerennialSeason>> SaveSeasonAsync(PerennialSeasonPreview preview, CancellationToken cancellationToken);
    Task<Result<int>> DeleteSeasonAsync(int planId, int expectedPlanVersion, PerennialPlantingId plantingId, CancellationToken cancellationToken);
}
public interface IManagePerennialPlanning
{
    Task<Result<PerennialPlantingFromViewModel>> SaveAsync(Option<int> id, int version, PerennialPlantingDefinition definition, CancellationToken cancellationToken = default);
    Task<Result<PerennialSeasonPreview>> CalculateAsync(int planId, int plantingId, GrowthPeriodOverrides manual, CancellationToken cancellationToken = default);
    Task<Result<SavedPerennialSeason>> SaveSeasonAsync(PerennialSeasonPreview preview, CancellationToken cancellationToken = default);
    Task<Result<int>> DeleteAsync(int id, int version, CancellationToken cancellationToken = default);
    Task<Result<int>> DeleteSeasonAsync(int planId, int expectedPlanVersion, int plantingId, CancellationToken cancellationToken = default);
}
public sealed class ManagePerennialPlanning(IPerennialPlanningReader reader, IPerennialPlanningWriter writer, IPlantingPlanReader plans, TimeProvider time) : IManagePerennialPlanning
{
    public Task<Result<PerennialPlantingFromViewModel>> SaveAsync(Option<int> id, int version, PerennialPlantingDefinition definition, CancellationToken cancellationToken = default)
        => id.Match(value => PerennialPlantingId.Create(value).Bind(key => Result.Success(Option.Some(key))), () => Result.Success(Option.None<PerennialPlantingId>()))
            .Match(key => key.Match(_ => version > 0, () => version == 0) ? writer.SaveAsync(new(key, version, definition), cancellationToken)
                : Task.FromResult(Result.Failure<PerennialPlantingFromViewModel>("Неверная версия посадки")),
                error => Task.FromResult(Result.Failure<PerennialPlantingFromViewModel>(error)));
    public async Task<Result<PerennialSeasonPreview>> CalculateAsync(int planId, int plantingId, GrowthPeriodOverrides manual, CancellationToken cancellationToken = default)
    {
        var key = SeasonalPlantingPlanId.Create(planId);
        if (key.IsFailure) return key.Bind(_ => Result.Failure<PerennialSeasonPreview>("Неверный ID плана"));
        var readPlan = await plans.ReadAsync(key.ValueOrThrow(), cancellationToken).ConfigureAwait(false);
        if (readPlan.IsFailure) return readPlan.Bind(_ => Result.Failure<PerennialSeasonPreview>("План недоступен"));
        var plan = readPlan.ValueOrThrow().Match<PlantingPlanDetailsFromViewModel?>(value => value, () => null);
        var allPlants = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (allPlants.IsFailure) return allPlants.Bind(_ => Result.Failure<PerennialSeasonPreview>("Посадки недоступны"));
        var planting = allPlants.ValueOrThrow().FirstOrDefault(x => x.Id == plantingId);
        if (plan is null || planting is null) return Result.Failure<PerennialSeasonPreview>("Выберите существующие план и посадку");
        if (plan.Definition.RegionId != planting.Definition.RegionId) return Result.Failure<PerennialSeasonPreview>("Регион посадки должен соответствовать региону плана");
        return SeasonalPhenology.Project(planting.Definition.Profile.Value, planting.Definition.EstablishmentYear, plan.Definition.SeasonYear,
            planting.Definition.FirstEmergence, manual).Bind(projection => Result.Success(new PerennialSeasonPreview(plan.Id, plan.Version, planting.Id, planting.Version,
                new(planting.Definition, manual, projection, time.GetUtcNow()))));
    }
    public Task<Result<SavedPerennialSeason>> SaveSeasonAsync(PerennialSeasonPreview preview, CancellationToken cancellationToken = default)
        => writer.SaveSeasonAsync(preview, cancellationToken);
    public Task<Result<int>> DeleteAsync(int id, int version, CancellationToken cancellationToken = default)
        => PerennialPlantingId.Create(id).Match(key => version > 0 ? writer.DeleteAsync(key, version, cancellationToken) : Task.FromResult(Result.Failure<int>("Неверная версия посадки")),
            error => Task.FromResult(Result.Failure<int>(error)));
    public Task<Result<int>> DeleteSeasonAsync(int planId, int expectedPlanVersion, int plantingId, CancellationToken cancellationToken = default)
        => PerennialPlantingId.Create(plantingId).Match(key => planId > 0 && expectedPlanVersion > 0 ? writer.DeleteSeasonAsync(planId, expectedPlanVersion, key, cancellationToken)
            : Task.FromResult(Result.Failure<int>("Неверный план или версия")), error => Task.FromResult(Result.Failure<int>(error)));
}
