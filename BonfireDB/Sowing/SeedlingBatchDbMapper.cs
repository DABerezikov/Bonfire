using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;
using BonfireDB.Entities;

namespace BonfireDB.Sowing;

public static class SeedlingBatchDbMapper
{
    public static Result<SeedlingBatch> Read(Seedling batch) => SeedlingBatch.Create(batch.Quantity,
        batch.SeedlingInfos.Select(info => new SeedlingEntry(Option.Some(info.Id), info.SeedlingNumber,
            info.GerminationDate.ToOption(), info.IsDead.ToOption())));

    public static void Apply(SeedlingBatch model, Seedling target)
    {
        var existing = target.SeedlingInfos.ToDictionary(info => info.Id);
        foreach (var entry in model.Entries)
        {
            var info = entry.Id.Match(id => existing[id], () =>
            {
                var added = new SeedlingInfo();
                target.SeedlingInfos.Add(added);
                return added;
            });
            info.SeedlingNumber = entry.Number;
            info.GerminationDate = entry.GerminationDate.ToNullable();
            info.IsDead = entry.IsDead.ToNullable();
        }
    }

    public static SeedlingChangeResultFromViewModel Receipt(Seedling batch) => new(batch.Id,
        batch.SeedlingInfos.Select(info => new SeedlingEntryFromViewModel(info.Id, info.SeedlingNumber,
            info.GerminationDate.ToOption(), info.IsDead.ToOption())).ToArray());
}
