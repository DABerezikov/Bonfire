using System.Collections.Immutable;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planning;

public sealed class DbSeedShoppingStockReader(IUnitOfWorkFactory units) : ISeedShoppingStockReader
{
    public Task<Result<ImmutableArray<SeedPurchaseStock>>> ReadAsync(ImmutableArray<SeedPurchaseKey> keys, CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            if (keys.IsEmpty) return Result.Success(ImmutableArray<SeedPurchaseStock>.Empty);
            await using var unit = units.Create();
            var cultures = keys.Select(key => key.CultureId.Value).Distinct().ToArray();
            var sorts = keys.Select(key => key.SortId.Value).Distinct().ToArray();
            var rows = await unit.Repository<Seed>().Rows.AsNoTracking()
                .Where(seed => cultures.Contains(seed.Plant.PlantCulture.Id) && sorts.Contains(seed.Plant.PlantSort.Id))
                .Select(seed => new { Culture = seed.Plant.PlantCulture.Id, Sort = seed.Plant.PlantSort.Id,
                    seed.SeedsInfo.AmountSeeds, seed.SeedsInfo.AmountSeedsWeight, seed.SeedsInfo.WeightPack })
                .ToArrayAsync(cancellationToken).ConfigureAwait(false);
            var pairs = keys.Select(key => (key.CultureId.Value, key.SortId.Value)).ToHashSet();
            return rows.Where(row => pairs.Contains((row.Culture, row.Sort))).Traverse(row => CultureId.Create(row.Culture).Bind(culture =>
                SortId.Create(row.Sort).Bind(sort => SeedPurchaseStock.Create(new(culture, sort), row.AmountSeeds,
                    row.AmountSeedsWeight.ToOption(), row.WeightPack))));
        }, "остатков семян для покупки");
}
