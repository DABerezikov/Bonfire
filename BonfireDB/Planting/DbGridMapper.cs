using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;

namespace BonfireDB.Planting;

public static class DbGridMapper
{
    public static GardenGrid Read(GardenElement element)
        => new(element.PlantingSpots.Select(spot => new GridPosition(spot.Row, spot.Column)));

    public static async Task ApplyAsync(GridChange change, GardenElement element, IUnitOfWork unit, CancellationToken cancellationToken = default)
    {
        var removed = change.Removed.ToHashSet();
        var spots = unit.Repository<PlantingSpot>();
        foreach (var spot in element.PlantingSpots.Where(spot => removed.Contains(new(spot.Row, spot.Column))).ToArray())
        {
            element.PlantingSpots.Remove(spot);
            await spots.RemoveAsync(spot.Id, cancellationToken).ConfigureAwait(false);
        }
        foreach (var position in change.Added)
        {
            var spot = new PlantingSpot { GardenElementId = element.Id, Row = position.Row, Column = position.Column };
            element.PlantingSpots.Add(spot);
            await spots.AddAsync(spot, cancellationToken).ConfigureAwait(false);
        }
        element.GridRows = change.Size.Rows;
        element.GridColumns = change.Size.Columns;
    }

    public static GridResultFromViewModel Receipt(GardenElement element) => new(element.Id, element.GridRows, element.GridColumns,
        element.PlantingSpots.OrderBy(spot => spot.Row).ThenBy(spot => spot.Column).Select(Receipt).ToArray());

    public static GridSpotFromViewModel Receipt(PlantingSpot spot) => new(
        spot.Id, spot.Row, spot.Column, spot.StateTypeName, spot.SeedlingInfoId.ToOption(),
        spot.PlantedDate.ToOption(), spot.HarvestDate.ToOption(), spot.Note.ToOption());
}
