using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;
using BonfireDB.Entities;

namespace BonfireDB.Sowing;

public static class SeedlingDetailsDbMapper
{
    public static void Apply(SeedlingDetails details, Seedling target)
    {
        target.Quantity = details.Values.Quantity;
        target.Weight = details.Values.Weight;
        target.LandingDate = details.LandingDate;
        target.SeedlingSource = details.Source;
        target.PlantPlace = details.Values.Place.ToNullable();
        target.LunarPhase = details.Values.LunarPhase;
        var metadata = target.SeedlingInfos[0];
        metadata.LandingDate = details.LandingDate;
        metadata.SeedlingSource = details.Source;
        metadata.PlantPlace = details.Values.Place.ToNullable();
        metadata.LunarPhase = details.Values.LunarPhase;
        metadata.Note = details.Values.Note.ToNullable();
    }
}
