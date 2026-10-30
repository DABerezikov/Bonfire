using System.Collections.Immutable;
using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planting;

public sealed class DbGardenReader(IUnitOfWorkFactory units) : IGardenReader
{
    public Task<Result<ImmutableArray<GardenPlanResultFromViewModel>>> ReadPlansAsync(CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var plans = await unit.Repository<GardenPlan>().Rows.AsNoTracking().OrderByDescending(p => p.Year)
                .ToArrayAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(plans.Select(GardenReadDbMapper.ReadPlan).ToImmutableArray());
        }, "планировщика");

    public Task<Result<ImmutableArray<GardenReadFromViewModel>>> ReadGardensAsync(GardenPlanId planId, CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            ArgumentNullException.ThrowIfNull(planId);
            await using var unit = units.Create();
            var gardens = await unit.Repository<Garden>().Rows
                .Include(g => g.Elements).ThenInclude(element => element.PlantingSpots)
                .Include(g => g.Greenhouses).ThenInclude(house => house.Elements).ThenInclude(element => element.PlantingSpots)
                .AsNoTracking().AsSplitQuery().Where(g => g.GardenPlanId == planId.Value)
                .ToArrayAsync(cancellationToken).ConfigureAwait(false);
            return gardens.Traverse(GardenReadDbMapper.ReadGarden);
        }, "планировщика");
}
