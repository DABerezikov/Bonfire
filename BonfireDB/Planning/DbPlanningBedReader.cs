using System.Collections.Immutable;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planning;

public sealed class DbPlanningBedReader(IUnitOfWorkFactory units) : IPlanningBedReader
{
    public Task<Result<ImmutableArray<PlanningBedFromViewModel>>> ReadAsync(CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var rows = await unit.Repository<GardenElement>().Rows.AsNoTracking().OfType<Bed>()
                .OrderBy(x => x.Id).Select(x => new { x.Id, x.Name, x.DisplayWidth, x.DisplayHeight })
                .ToArrayAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(rows
                .Select(x => new PlanningBedFromViewModel(x.Id, x.Name, x.DisplayWidth, x.DisplayHeight))
                .ToImmutableArray());
        }, "размеров грядок");
}