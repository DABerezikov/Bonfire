using System.Collections.Immutable;
using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.GardenPlanning;

namespace BonfireDB.Planting;

internal static class GreenhousePropertiesDbMapper
{
    public static Result<GreenhousePropertiesState> Read(Greenhouse house)
        => CanvasRectangle.Create(0, 0, house.CanvasWidth, house.CanvasHeight).Bind(canvas =>
            Result.Success(new GreenhousePropertiesState(house.Id,
                new GreenhouseEditorValues(house.Name, house.Note.ToOption(), house.Material.ToOption(),
                    house.WidthMeters, house.HeightMeters), canvas)));

    public static Result<GreenhouseContents> Contents(Greenhouse house)
        => house.Elements.Aggregate(Result.Success(ImmutableList<InteriorElement>.Empty), (snapshot, element) =>
                snapshot.Bind(elements => CanvasRectangle
                    .Create(element.X, element.Y, element.DisplayWidth, element.DisplayHeight)
                    .Bind(rectangle => Result.Success(elements.Add(new InteriorElement(element.Name, rectangle))))))
            .Bind(elements => Result.Success(new GreenhouseContents(elements)));

    public static void Apply(GreenhousePropertiesState state, Greenhouse house)
    {
        house.Name = state.Values.Name;
        house.Note = state.Values.Note.ToNullable();
        house.Material = state.Values.Material.ToNullable();
        house.WidthMeters = state.Values.WidthMeters;
        house.HeightMeters = state.Values.HeightMeters;
        house.CanvasWidth = state.Canvas.Width;
        house.CanvasHeight = state.Canvas.Height;
    }

    public static GreenhousePropertiesResultFromViewModel Receipt(GreenhousePropertiesState state)
        => new(state.Id, state.Values, state.Canvas);
}