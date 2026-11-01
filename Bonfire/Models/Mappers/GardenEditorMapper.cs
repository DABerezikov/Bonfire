using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Models.Mappers;

internal static class GardenEditorMapper
{
    public static GardenEditorDraft Open(GardenFromViewModel garden)
        => new(garden.Id,
            new GardenEditorValues(garden.Name, garden.Note.ToOption(), garden.Address.ToOption(), garden.WidthMeters,
                garden.HeightMeters));

    public static void Apply(GardenPropertiesResultFromViewModel saved, GardenFromViewModel garden)
    {
        if (garden.Name != saved.Values.Name) garden.Name = saved.Values.Name;
        garden.Note = saved.Values.Note.ToNullable();
        garden.Address = saved.Values.Address.ToNullable();
        if (garden.WidthMeters != saved.Values.WidthMeters) garden.WidthMeters = saved.Values.WidthMeters;
        if (garden.HeightMeters != saved.Values.HeightMeters) garden.HeightMeters = saved.Values.HeightMeters;
        if (garden.CanvasWidth != saved.Canvas.Width) garden.CanvasWidth = saved.Canvas.Width;
        if (garden.CanvasHeight != saved.Canvas.Height) garden.CanvasHeight = saved.Canvas.Height;
        foreach (var child in garden.Greenhouses)
        {
            child.ContainerCanvasWidth = saved.Canvas.Width;
            child.ContainerCanvasHeight = saved.Canvas.Height;
        }

        foreach (var child in garden.Elements)
        {
            child.ContainerCanvasWidth = saved.Canvas.Width;
            child.ContainerCanvasHeight = saved.Canvas.Height;
        }
    }
}