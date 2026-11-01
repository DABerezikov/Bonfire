using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.GardenPlanning;

namespace BonfireDB.Planting;

public static class PlantingCellDbMapper
{
    public static PlantingCell Read(PlantingSpot spot) => new(spot.State, spot.SeedlingInfoId.ToOption(),
        spot.PlantedDate.ToOption(), spot.HarvestDate.ToOption(), spot.Note.ToOption());

    public static void Apply(PlantingCell cell, PlantingSpot spot)
    {
        spot.StateTypeName = cell.State.GetType().Name;
        spot.SeedlingInfoId = cell.SeedlingInfoId.ToNullable();
        spot.PlantedDate = cell.PlantedDate.ToNullable();
        spot.HarvestDate = cell.HarvestDate.ToNullable();
        spot.Note = cell.Note.ToNullable();
    }
}