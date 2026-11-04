using System.Collections.Immutable;
using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Sowing;

public sealed class DbSeedlingReader(IUnitOfWorkFactory units) : ISeedlingReader
{
    public Task<Result<ImmutableArray<SeedlingReadFromViewModel>>> ReadAsync(CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            return (await Query(unit).ToArrayAsync(cancellationToken).ConfigureAwait(false)).Traverse(
                SeedlingReadDbMapper.Read);
        }, "рассады");

    public Task<Result<Option<SeedlingReadFromViewModel>>> ReadAsync(int id, CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            if (id <= 0) return Result.Failure<Option<SeedlingReadFromViewModel>>("Некорректный идентификатор рассады");
            await using var unit = units.Create();
            var row = await Query(unit).SingleOrDefaultAsync(batch => batch.Id == id, cancellationToken)
                .ConfigureAwait(false);
            return row is null
                ? Result.Success(Option.None<SeedlingReadFromViewModel>())
                : SeedlingReadDbMapper.Read(row).Bind(snapshot => Result.Success(Option.Some(snapshot)));
        }, "рассады");

    private static IQueryable<Seedling> Query(IUnitOfWork unit) => unit.Repository<Seedling>().Rows
        .Include(batch => batch.Plant.PlantCulture).Include(batch => batch.Plant.PlantSort.Producer)
        .Include(batch => batch.SeedlingInfos).ThenInclude(info => info.Replants)
        .Include(batch => batch.SeedlingInfos).ThenInclude(info => info.Treatments).AsNoTracking().AsSplitQuery();
}