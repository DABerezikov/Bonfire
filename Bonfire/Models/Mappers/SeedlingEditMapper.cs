using System.Linq;
using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;

namespace Bonfire.Models.Mappers;

public static class SeedlingEditMapper
{
    public static SeedlingEditorDraft Create(SeedlingReadModel batch)
    {
        var metadata = batch.SeedlingInfos.FirstOrDefault();
        var values = new SeedlingDetailsValues
        {
            Quantity = batch.Quantity, Weight = batch.Weight,
            LandingDate = (batch.LandingDate ?? metadata?.LandingDate).ToOption(),
            Source = (batch.SeedlingSource ?? metadata?.SeedlingSource).ToOption(),
            Place = (batch.PlantPlace ?? metadata?.PlantPlace).ToOption(), Note = (metadata?.Note).ToOption(),
            LunarPhase = batch.LunarPhase ?? metadata?.LunarPhase ?? string.Empty
        };
        return new(batch.Id, values, batch.SeedlingInfos.Select(info => new SeedlingEntryEditModel(
            new(info.Id, info.SeedlingNumber, info.GerminationDate.ToOption(), info.DeathNote.ToOption()))));
    }

    public static EditSeedlingFromViewModel ToRequest(SeedlingEditorDraft draft)
        => new(draft.Id, draft.Values, draft.Entries.Select(entry => entry.Values).ToArray());
}
