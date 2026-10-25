using System.Collections.Immutable;
using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using BonfireDB.Entities.Base;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Sowing;

public sealed class DbSeedlingReader(IUnitOfWorkFactory units) : ISeedlingReader
{
    private static IQueryable<Seedling> Query(IUnitOfWork unit) => unit.Repository<Seedling>().Rows
        .Include(batch => batch.Plant.PlantCulture).Include(batch => batch.Plant.PlantSort.Producer)
        .Include(batch => batch.SeedlingInfos).ThenInclude(info => info.Replants)
        .Include(batch => batch.SeedlingInfos).ThenInclude(info => info.Treatments).AsNoTracking().AsSplitQuery();

    public async Task<Result<ImmutableArray<SeedlingReadFromViewModel>>> ReadAsync(CancellationToken cancellationToken)
    {
        await using var unit = units.Create();
        return (await Query(unit).ToArrayAsync(cancellationToken).ConfigureAwait(false)).Traverse(SeedlingReadDbMapper.Read);
    }

    public async Task<Result<Option<SeedlingReadFromViewModel>>> ReadAsync(int id, CancellationToken cancellationToken)
    {
        if (id <= 0) return Result.Failure<Option<SeedlingReadFromViewModel>>("Некорректный идентификатор рассады");
        await using var unit = units.Create();
        var row = await Query(unit).SingleOrDefaultAsync(batch => batch.Id == id, cancellationToken).ConfigureAwait(false);
        return row is null ? Result.Success(Option.None<SeedlingReadFromViewModel>())
            : SeedlingReadDbMapper.Read(row).Bind(snapshot => Result.Success(Option.Some(snapshot)));
    }
}
