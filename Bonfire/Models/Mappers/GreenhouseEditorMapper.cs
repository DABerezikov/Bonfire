using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;

namespace Bonfire.Models.Mappers;

internal static class GreenhouseEditorMapper
{
    public static GreenhouseEditorDraft Open(GreenhouseFromViewModel house)
        => new(house.Id, new(house.Name, house.Note.ToOption(), house.Material.ToOption(), house.WidthMeters, house.HeightMeters));

    public static void Apply(GreenhousePropertiesResultFromViewModel saved, GreenhouseFromViewModel house)
    {
        if (house.Name != saved.Values.Name) house.Name = saved.Values.Name;
        house.Note = saved.Values.Note.ToNullable();
        house.Material = saved.Values.Material.ToNullable();
        if (house.WidthMeters != saved.Values.WidthMeters) house.WidthMeters = saved.Values.WidthMeters;
        if (house.HeightMeters != saved.Values.HeightMeters) house.HeightMeters = saved.Values.HeightMeters;
        if (house.InnerCanvasWidth != saved.Canvas.Width) house.InnerCanvasWidth = saved.Canvas.Width;
        if (house.InnerCanvasHeight != saved.Canvas.Height) house.InnerCanvasHeight = saved.Canvas.Height;
        foreach (var child in house.InnerElements)
        {
            child.ContainerCanvasWidth = saved.Canvas.Width;
            child.ContainerCanvasHeight = saved.Canvas.Height;
        }
    }
}
