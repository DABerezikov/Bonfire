using System;
using System.Collections.Immutable;
using System.Linq;
using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;

namespace Bonfire.Models.Mappers;

internal static class SeedlingReadMapper
{
    public static SeedlingReadModel Apply(SeedlingReadModel current, SeedlingChangeResultFromViewModel receipt,
        Option<SeedlingDetails> details)
    {
        var entries = current.Snapshot.SeedlingInfos.ToBuilder();
        foreach (var entry in receipt.Entries)
        {
            var index = Enumerable.FirstOrDefault(Enumerable.Range(0, entries.Count),
                i => entries[i].Entry.Id == entry.Id, -1);
            if (index >= 0) entries[index] = entries[index] with { Entry = entry };
            else
                entries.Add(new SeedlingInfoReadFromViewModel(entry, Option.None<int>(), default, string.Empty,
                    Option.None<string>(),
                    Option.None<DateTime>(), Option.None<DateTime>(), Option.None<string>(), Option.None<string>(),
                    string.Empty, Option.None<string>(), Option.None<DateTime>(),
                    Option.None<ImmutableArray<TreatmentReadFromViewModel>>()));
        }

        var snapshot = current.Snapshot with { PlantedOut = receipt.PlantedOut, SeedlingInfos = entries.ToImmutable() };
        return details.Match(value =>
        {
            if (snapshot.SeedlingInfos.IsEmpty)
                throw new InvalidOperationException("В партии нет метаданных для применения результата");
            var metadata = snapshot.SeedlingInfos[0] with
            {
                LandingDate = value.LandingDate, SeedlingSource = value.Source,
                PlantPlace = value.Values.Place, LunarPhase = value.Values.LunarPhase, Note = value.Values.Note
            };
            return new SeedlingReadModel(snapshot with
            {
                Quantity = value.Values.Quantity, Weight = value.Values.Weight,
                LandingDate = Option.Some(value.LandingDate),
                SeedlingSource = Option.Some(value.Source), PlantPlace = value.Values.Place,
                LunarPhase = Option.Some(value.Values.LunarPhase),
                SeedlingInfos = snapshot.SeedlingInfos.SetItem(0, metadata)
            });
        }, () => new SeedlingReadModel(snapshot));
    }
}