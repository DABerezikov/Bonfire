using System.Collections.Immutable;
using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Planning;

public sealed record SeasonalPlantingPlan
{
    private SeasonalPlantingPlan(SeasonalPlantingPlanId id, int seasonYear, RegionId regionId,
        ImmutableArray<PlannedBatch> batches)
        => (Id, SeasonYear, RegionId, Batches) = (id, seasonYear, regionId, batches);

    public SeasonalPlantingPlanId Id { get; }
    public int SeasonYear { get; }
    public RegionId RegionId { get; }
    public ImmutableArray<PlannedBatch> Batches { get; }

    public static Result<SeasonalPlantingPlan> Create(SeasonalPlantingPlanId id, int seasonYear,
        RegionId regionId, ImmutableArray<PlannedBatch> batches)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(regionId);
        if (seasonYear is < 1 or > 9999)
            return Result.Failure<SeasonalPlantingPlan>("Год сезона должен быть от 1 до 9999");
        if (batches.IsDefault || batches.Any(batch => batch is null)
                              || batches.Select(batch => batch.Id).Distinct().Count() != batches.Length)
            return Result.Failure<SeasonalPlantingPlan>("Партии не заданы либо повторяются");
        return Result.Success(new SeasonalPlantingPlan(id, seasonYear, regionId, batches));
    }

    public Result<SeasonalPlantingPlan> WithBatch(PlannedBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        var index = -1;
        for (var i = 0; i < Batches.Length; i++)
            if (Batches[i].Id == batch.Id)
            {
                index = i;
                break;
            }

        return index < 0
            ? Result.Failure<SeasonalPlantingPlan>("Партия не принадлежит этому плану")
            : Create(Id, SeasonYear, RegionId, Batches.SetItem(index, batch));
    }
}