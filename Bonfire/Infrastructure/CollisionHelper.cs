using System.Collections.Generic;
using System.Linq;
using Bonfire.Models;

namespace Bonfire.Infrastructure;

/// <summary>
/// Проверка перекрытия прямоугольников на холсте.
/// Правило: граница на границу допустима; запрещено только строгое наложение (общая площадь > 0).
/// </summary>
internal static class CollisionHelper
{
    /// <summary>
    /// Строгое пересечение двух прямоугольников (касание границами = НЕ перекрытие).
    /// </summary>
    public static bool Overlaps(double ax, double ay, double aw, double ah,
                                double bx, double by, double bw, double bh)
        => ax < bx + bw && ax + aw > bx
        && ay < by + bh && ay + ah > by;

    /// <summary>Проверяет пересечение с любым элементом коллекции (кроме себя).</summary>
    public static bool CollidesWithAny(
        double x, double y, double w, double h,
        IEnumerable<GardenElementFromViewModel> elements,
        GardenElementFromViewModel? exclude = null)
        => elements
            .Where(e => e != exclude)
            .Any(e => Overlaps(x, y, w, h, e.X, e.Y, e.Width, e.Height));

    /// <summary>Проверяет пересечение с любой теплицей.</summary>
    public static bool CollidesWithAny(
        double x, double y, double w, double h,
        IEnumerable<GreenhouseFromViewModel> greenhouses)
        => greenhouses.Any(gh =>
            Overlaps(x, y, w, h, gh.X, gh.Y, gh.DisplayWidth, gh.DisplayHeight));

    /// <summary>
    /// Проверяет пересечение с элементами и теплицами одновременно.
    /// </summary>
    public static bool CollidesWithSiblings(
        double x, double y, double w, double h,
        GardenElementFromViewModel vm)
    {
        if (vm.ContainerElements is not null
            && CollidesWithAny(x, y, w, h, vm.ContainerElements, vm))
            return true;
        if (vm.ContainerGreenhouses is not null
            && CollidesWithAny(x, y, w, h, vm.ContainerGreenhouses))
            return true;
        return false;
    }

    /// <summary>
    /// Ищет первое свободное место для прямоугольника (w × h) на холсте.
    /// Теплицы учитываются наравне с элементами.
    /// Кандидаты — углы по краям уже размещённых объектов, перебор сверху-вниз/слева-направо.
    /// Возвращает null, если свободного места нет.
    /// </summary>
    public static (double x, double y)? FindFreeSpot(
        IEnumerable<GardenElementFromViewModel> elements,
        IEnumerable<GreenhouseFromViewModel>? greenhouses,
        GardenElementFromViewModel? exclude,
        double w, double h,
        double canvasW, double canvasH)
    {
        var others = elements.Where(e => e != exclude).ToList();
        var ghList = greenhouses?.ToList() ?? [];

        var xs = new SortedSet<double> { 0 };
        var ys = new SortedSet<double> { 0 };

        foreach (var e in others)
        {
            xs.Add(e.X + e.Width);
            if (e.X - w >= 0) xs.Add(e.X - w);
            ys.Add(e.Y + e.Height);
            if (e.Y - h >= 0) ys.Add(e.Y - h);
        }
        foreach (var gh in ghList)
        {
            xs.Add(gh.X + gh.DisplayWidth);
            if (gh.X - w >= 0) xs.Add(gh.X - w);
            ys.Add(gh.Y + gh.DisplayHeight);
            if (gh.Y - h >= 0) ys.Add(gh.Y - h);
        }

        foreach (var cy in ys)
        foreach (var cx in xs)
        {
            if (cx < 0 || cy < 0 || cx + w > canvasW || cy + h > canvasH) continue;
            if (others.All(e => !Overlaps(cx, cy, w, h, e.X, e.Y, e.Width, e.Height))
             && ghList.All(gh => !Overlaps(cx, cy, w, h, gh.X, gh.Y, gh.DisplayWidth, gh.DisplayHeight)))
                return (cx, cy);
        }
        return null;
    }
}
