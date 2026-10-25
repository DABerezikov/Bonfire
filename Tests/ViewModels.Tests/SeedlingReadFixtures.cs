using System.Collections.Immutable;
using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using BonfireDB.Sowing;

namespace ViewModels.Tests;

internal static class SeedlingReadFixtures
{
    public static Result<ImmutableArray<SeedlingReadFromViewModel>> ReadSeedlings(this IEnumerable<Seedling> batches)
        => batches.Traverse(batch =>
        {
            // В фикстуре нет SQLite-генератора ключей: назначаем ключи новым строкам до маппинга.
            var next = batch.SeedlingInfos.Select(info => info.Id).DefaultIfEmpty().Max() + 1;
            foreach (var info in batch.SeedlingInfos.Where(info => info.Id == 0)) info.Id = next++;
            return SeedlingReadDbMapper.Read(batch);
        });
}
