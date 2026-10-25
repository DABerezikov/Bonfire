using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Models.Mappers;

internal static class CanvasGeometryMapper
{
    public static Result<CanvasRectangle> Read(GardenElementFromViewModel element)
        => CanvasRectangle.Create(element.X, element.Y, element.Width, element.Height);

    public static Result<CanvasRectangle> Read(GreenhouseFromViewModel greenhouse)
        => CanvasRectangle.Create(greenhouse.X, greenhouse.Y, greenhouse.DisplayWidth, greenhouse.DisplayHeight);

    public static Result<CanvasBounds> ReadBounds(double width, double height)
        => CanvasBounds.Create(KnownBound(width), KnownBound(height));

    // На прежней границе UI неположительный размер означает неизвестный контейнер.
    private static Option<double> KnownBound(double value)
        => value > 0 ? Option.Some(value) : Option.None<double>();
}
