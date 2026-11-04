using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Models.Mappers;

internal static class CanvasGeometryMapper
{
    public static Result<(decimal Length, decimal Width)> PlanningSize(double width, double height,
        double pixelsPerMeter)
    {
        if (!double.IsFinite(width) || !double.IsFinite(height) || !double.IsFinite(pixelsPerMeter) || width <= 0 ||
            height <= 0 || pixelsPerMeter <= 0)
            return Result.Failure<(decimal, decimal)>(
                "Размеры грядки и масштаб должны быть конечными и положительными");

        var lengthMeters = width / pixelsPerMeter;
        var widthMeters = height / pixelsPerMeter;

        if (!double.IsFinite(lengthMeters) || !double.IsFinite(widthMeters) ||
            lengthMeters >= (double)decimal.MaxValue || widthMeters >= (double)decimal.MaxValue)
            return Result.Failure<(decimal, decimal)>("Размеры грядки выходят за диапазон");

        var length = (decimal)lengthMeters;
        var actualWidth = (decimal)widthMeters;

        return length > 0 && actualWidth > 0
            ? Result.Success((length, actualWidth))
            : Result.Failure<(decimal, decimal)>("Размеры грядки слишком малы для расчёта");
    }

    public static Result<CanvasRectangle> Read(GardenElementFromViewModel element)
        => CanvasRectangle.Create(element.X, element.Y, element.Width, element.Height);

    public static Result<CanvasRectangle> Read(GreenhouseFromViewModel greenhouse)
        => CanvasRectangle.Create(greenhouse.X, greenhouse.Y, greenhouse.DisplayWidth, greenhouse.DisplayHeight);

    public static void Apply(CanvasRectangle rectangle, GardenElementFromViewModel element)
    {
        if (element.X != rectangle.X) element.X = rectangle.X;
        if (element.Y != rectangle.Y) element.Y = rectangle.Y;
        if (element.Width != rectangle.Width) element.Width = rectangle.Width;
        if (element.Height != rectangle.Height) element.Height = rectangle.Height;
    }

    public static void Apply(CanvasRectangle rectangle, GreenhouseFromViewModel greenhouse)
    {
        if (greenhouse.X != rectangle.X) greenhouse.X = rectangle.X;
        if (greenhouse.Y != rectangle.Y) greenhouse.Y = rectangle.Y;
        if (greenhouse.DisplayWidth != rectangle.Width) greenhouse.DisplayWidth = rectangle.Width;
        if (greenhouse.DisplayHeight != rectangle.Height) greenhouse.DisplayHeight = rectangle.Height;
    }

    public static Result<CanvasBounds> ReadBounds(double width, double height)
        => CanvasBounds.Create(KnownBound(width), KnownBound(height));

    // На прежней границе UI неположительный размер означает неизвестный контейнер.
    private static Option<double> KnownBound(double value)
        => value > 0 ? Option.Some(value) : Option.None<double>();
}