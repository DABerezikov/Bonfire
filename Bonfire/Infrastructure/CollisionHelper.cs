using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Models;

namespace Bonfire.Infrastructure;

/// <summary>Граница UI: снимок геометрии, исключение себя и вызов чистого домена.</summary>
internal static class CollisionHelper
{
    public static bool Overlaps(double ax, double ay, double aw, double ah,
        double bx, double by, double bw, double bh)
        => CanvasIntersection.Overlaps(ax, ay, aw, ah, bx, by, bw, bh);

    public static bool CollidesWithAny(double x, double y, double w, double h,
        IEnumerable<GardenElementFromViewModel> elements, GardenElementFromViewModel? exclude = null)
        => elements.Where(element => element != exclude)
            .Any(element => Overlaps(x, y, w, h, element.X, element.Y, element.Width, element.Height));

    public static bool CollidesWithAny(double x, double y, double w, double h,
        IEnumerable<GreenhouseFromViewModel> greenhouses)
        => greenhouses.Any(greenhouse => Overlaps(x, y, w, h, greenhouse.X, greenhouse.Y, greenhouse.DisplayWidth, greenhouse.DisplayHeight));

    public static bool CollidesWithSiblings(double x, double y, double w, double h, GardenElementFromViewModel vm)
        => CollidesWithAny(x, y, w, h, vm.ContainerElements ?? [], vm)
            || CollidesWithAny(x, y, w, h, vm.ContainerGreenhouses ?? []);

    public static bool CollidesWithSiblings(double x, double y, double w, double h, GreenhouseFromViewModel vm)
        => CollidesWithAny(x, y, w, h, vm.ContainerElements ?? [])
            || CollidesWithAny(x, y, w, h, (vm.ContainerGreenhouses ?? []).Where(greenhouse => greenhouse != vm));

    public static Result<Option<(double x, double y)>> FindPlacement(
        IEnumerable<GardenElementFromViewModel> elements,
        IEnumerable<GreenhouseFromViewModel>? greenhouses,
        GardenElementFromViewModel? exclude,
        double w, double h, double canvasW, double canvasH)
        => ReadLayout(elements.Where(element => element != exclude).Select(Rectangle)
                .Concat((greenhouses ?? []).Select(Rectangle)))
            .Bind(layout => layout.FindFreeSpot(w, h, canvasW, canvasH))
            .Bind(space => Result.Success(space.Map(rectangle => (x: rectangle.X, y: rectangle.Y))));

    // Совместимость прежних потребителей: nullable только на границе UI.
    public static (double x, double y)? FindFreeSpot(
        IEnumerable<GardenElementFromViewModel> elements,
        IEnumerable<GreenhouseFromViewModel>? greenhouses,
        GardenElementFromViewModel? exclude,
        double w, double h, double canvasW, double canvasH)
        => FindPlacement(elements, greenhouses, exclude, w, h, canvasW, canvasH).ValueOrThrow().ToNullable();

    private static Result<CanvasRectangle> Rectangle(GardenElementFromViewModel element)
        => CanvasRectangle.Create(element.X, element.Y, element.Width, element.Height);

    private static Result<CanvasRectangle> Rectangle(GreenhouseFromViewModel greenhouse)
        => CanvasRectangle.Create(greenhouse.X, greenhouse.Y, greenhouse.DisplayWidth, greenhouse.DisplayHeight);

    private static Result<PlotLayout> ReadLayout(IEnumerable<Result<CanvasRectangle>> obstacles)
        => obstacles.Aggregate(Result.Success(ImmutableList<CanvasRectangle>.Empty),
                (snapshot, obstacle) => snapshot.Bind(items => obstacle.Bind(rectangle => Result.Success(items.Add(rectangle)))))
            .Bind(items => Result.Success(new PlotLayout(items)));
}
