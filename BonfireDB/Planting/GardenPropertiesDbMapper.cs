using System.Collections.Immutable;
using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.GardenPlanning;

namespace BonfireDB.Planting;

internal static class GardenPropertiesDbMapper
{
    public static Result<GardenPropertiesState> Read(Garden garden)
        => CanvasRectangle.Create(0, 0, garden.CanvasWidth, garden.CanvasHeight).Bind(canvas =>
            Result.Success(new GardenPropertiesState(garden.Id,
                new(garden.Name, garden.Note.ToOption(), garden.Address.ToOption(), garden.WidthMeters, garden.HeightMeters), canvas)));

    private static Result<ImmutableList<InteriorElement>> ReadContents(IEnumerable<(string Name, double X, double Y, double Width, double Height)> children)
        => children.Aggregate(Result.Success(ImmutableList<InteriorElement>.Empty), (snapshot, child) =>
            snapshot.Bind(items => CanvasRectangle.Create(child.X, child.Y, child.Width, child.Height)
                .Bind(rectangle => Result.Success(items.Add(new(child.Name, rectangle))))));

    public static Result<GardenContents> Contents(Garden garden)
        => ReadContents(garden.Elements.Select(e => (e.Name, e.X, e.Y, e.DisplayWidth, e.DisplayHeight)))
            .Bind(elements => ReadContents(garden.Greenhouses.Select(h => (h.Name, h.X, h.Y, h.DisplayWidth, h.DisplayHeight)))
                .Bind(houses => Result.Success(new GardenContents(elements, houses))));

    public static void Apply(GardenPropertiesState state, Garden garden)
    {
        garden.Name = state.Values.Name;
        garden.Note = state.Values.Note.ToNullable();
        garden.Address = state.Values.Address.ToNullable();
        garden.WidthMeters = state.Values.WidthMeters;
        garden.HeightMeters = state.Values.HeightMeters;
        garden.CanvasWidth = state.Canvas.Width;
        garden.CanvasHeight = state.Canvas.Height;
    }

    public static GardenPropertiesResultFromViewModel Receipt(GardenPropertiesState state)
        => new(state.Id, state.Values, state.Canvas);
}
