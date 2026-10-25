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
    public async Task<Result<ImmutableArray<GardenPlanResultFromViewModel>>> ReadPlansAsync(CancellationToken cancellationToken)
    {
        await using var unit = units.Create();
        var plans = await unit.Repository<GardenPlan>().Items.AsNoTracking().OrderByDescending(p => p.Year)
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(plans.Select(GardenReadDbMapper.ReadPlan).ToImmutableArray());
    }

    public async Task<Result<ImmutableArray<GardenReadFromViewModel>>> ReadGardensAsync(GardenPlanId planId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(planId);
        await using var unit = units.Create();
        var gardens = await unit.Repository<Garden>().Items.AsNoTracking().AsSplitQuery().Where(g => g.GardenPlanId == planId.Value)
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);
        return gardens.Traverse(GardenReadDbMapper.ReadGarden);
    }
}
