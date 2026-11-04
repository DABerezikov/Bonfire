using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Models.Mappers;

internal static class ElementEditorMapper
{
    public static ElementEditorDraft Open(GardenElementFromViewModel element) => new(element.Id, Read(element));

    public static ElementEditorValues Read(GardenElementFromViewModel element) => new(
        new ElementDetailsValues(element.Name, element.Note.ToOption(), element.SoilType.ToOption(),
            element.Orientation.ToOption(), element.CoverMaterial.ToOption()),
        element.X, element.Y, element.Width, element.Height);

    public static ElementEditorValues Values(ElementPropertiesResultFromViewModel saved)
        => new(saved.Details, saved.Rectangle.X, saved.Rectangle.Y, saved.Rectangle.Width, saved.Rectangle.Height);

    public static void Apply(ElementPropertiesResultFromViewModel saved, GardenElementFromViewModel element)
    {
        CanvasGeometryMapper.Apply(saved.Rectangle, element);
        if (element.Name != saved.Details.Name) element.Name = saved.Details.Name;
        element.Note = saved.Details.Note.ToNullable();
        element.SoilType = saved.Details.SoilType.ToNullable();
        element.Orientation = saved.Details.Orientation.ToNullable();
        element.CoverMaterial = saved.Details.CoverMaterial.ToNullable();
    }
}