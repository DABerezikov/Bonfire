using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.GardenPlanning;

namespace BonfireDB.Planting;

internal static class ElementPropertiesDbMapper
{
    public static Result<ElementPropertiesState> Read(GardenElement element)
        => CanvasRectangle.Create(element.X, element.Y, element.DisplayWidth, element.DisplayHeight).Bind(rectangle =>
            Result.Success(new ElementPropertiesState(element.Id, new(element.Name, element.Note.ToOption(), element.SoilType.ToOption(),
                element is Bed bed ? bed.Orientation.ToOption() : Option.None<string>(),
                element is ColdFrame frame ? frame.CoverMaterial.ToOption() : Option.None<string>()), rectangle, element.AreaSquareMeters, element.IsLocked)));

    public static void Apply(ElementPropertiesState state, GardenElement element)
    {
        (element.X, element.Y, element.DisplayWidth, element.DisplayHeight) = (state.Rectangle.X, state.Rectangle.Y, state.Rectangle.Width, state.Rectangle.Height);
        element.AreaSquareMeters = state.AreaSquareMeters;
        element.Name = state.Details.Name;
        element.Note = state.Details.Note.ToNullable();
        element.SoilType = state.Details.SoilType.ToNullable();
        if (element is Bed bed) bed.Orientation = state.Details.Orientation.ToNullable();
        if (element is ColdFrame frame) frame.CoverMaterial = state.Details.CoverMaterial.ToNullable();
    }

    public static Result<ElementPropertiesResultFromViewModel> Receipt(GardenElement element)
        => Read(element).Bind(state => Result.Success(new ElementPropertiesResultFromViewModel(state.Id, state.Details, state.Rectangle, state.AreaSquareMeters)));
}
