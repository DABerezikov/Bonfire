using System.Collections.Generic;
using System.Linq;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Models;
using Bonfire.Models.Mappers;

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
        => greenhouses.Any(greenhouse => Overlaps(x, y, w, h, greenhouse.X, greenhouse.Y, greenhouse.DisplayWidth,
            greenhouse.DisplayHeight));

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
        => PlotLayout.Create(elements.Where(element => element != exclude).Select(CanvasGeometryMapper.Read)
                .Concat((greenhouses ?? []).Select(CanvasGeometryMapper.Read)))
            .Bind(layout => layout.FindFreeSpot(w, h, canvasW, canvasH))
            .Bind(space => Result.Success(space.Map(rectangle => (x: rectangle.X, y: rectangle.Y))));
}