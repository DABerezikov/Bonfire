using System.Collections.Immutable;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using BonfireDB.Entities.Base;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.SeedEditing;

public sealed class DbSeedReader(IUnitOfWorkFactory units) : ISeedReader
{
    public async Task<Result<ImmutableArray<SeedReadFromViewModel>>> ReadAsync(CancellationToken cancellationToken)
    {
        await using var unit = units.Create();
        var seeds = await unit.Repository<Seed>().Rows.Include(seed => seed.SeedsInfo)
            .Include(seed => seed.Plant.PlantCulture).Include(seed => seed.Plant.PlantSort.Producer)
            .AsNoTracking().AsSplitQuery().ToArrayAsync(cancellationToken).ConfigureAwait(false);
        return seeds.Traverse(SeedReadDbMapper.Read);
    }
}
