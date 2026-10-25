using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;
using BonfireDB.Entities;
using System.Collections.Immutable;

namespace BonfireDB.Sowing;

public static class SeedlingBatchDbMapper
{
    public static Result<SeedlingBatch> Read(Seedling batch) => SeedlingBatch.Create(batch.Quantity,
        batch.SeedlingInfos.Select(info => new SeedlingEntry(Option.Some(info.Id), info.SeedlingNumber,
            info.GerminationDate.ToOption(), info.IsDead.ToOption())
        {
            DeathNote = info.DeathNote.ToOption(),
            Replants = (info.Replants ?? []).Select(replant => new ReplantingEntry(Option.Some(replant.Id),
                replant.ReplantingDate, replant.PotVolume.ToOption(), replant.ReplantingNote.ToOption())).ToImmutableArray()
        }), batch.PlantedOut);

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
            info.DeathNote = entry.DeathNote.ToNullable();
            var replants = (info.Replants ?? []).ToDictionary(replant => replant.Id);
            info.Replants ??= [];
            foreach (var replant in entry.Replants)
            {
                var stored = replant.Id.Match(id => replants[id], () =>
                {
                    var added = new Replanting { SeedlingInfo = info };
                    info.Replants.Add(added);
                    return added;
                });
                stored.ReplantingDate = replant.Date;
                stored.PotVolume = replant.PotVolume.ToNullable();
                stored.ReplantingNote = replant.Note.ToNullable();
            }
        }
    }

    public static SeedlingChangeResultFromViewModel Receipt(Seedling batch) => new(batch.Id,
        batch.SeedlingInfos.Select(info => new SeedlingEntryFromViewModel(info.Id, info.SeedlingNumber,
            info.GerminationDate.ToOption(), info.IsDead.ToOption())
        {
            DeathNote = info.DeathNote.ToOption(),
            Replants = (info.Replants ?? []).Select(replant => new ReplantingEntryFromViewModel(replant.Id,
                replant.ReplantingDate, replant.PotVolume.ToOption(), replant.ReplantingNote.ToOption())).ToArray()
        }).ToArray()) { PlantedOut = batch.PlantedOut };
}
