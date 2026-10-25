using System.Collections.Immutable;
using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Garden;

/// <summary>Прежний численный предикат для совместимости; новый код использует проверенный CanvasRectangle.</summary>
public static class CanvasIntersection
{
    public static bool Overlaps(double ax, double ay, double aw, double ah, double bx, double by, double bw, double bh)
        => ax < bx + bw && ax + aw > bx && ay < by + bh && ay + ah > by;
}

public sealed record CanvasRectangle
{
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public double Right => X + Width;
    public double Bottom => Y + Height;

    private CanvasRectangle(double x, double y, double width, double height)
        => (X, Y, Width, Height) = (x, y, width, height);

    public static Result<CanvasRectangle> Create(double x, double y, double width, double height)
        => !double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(width) || !double.IsFinite(height)
            || width < 0 || height < 0 || !double.IsFinite(x + width) || !double.IsFinite(y + height)
                ? Result.Failure<CanvasRectangle>("Координаты и размеры должны быть конечными, размеры — неотрицательными")
                : Result.Success(new CanvasRectangle(x, y, width, height));

    public bool Overlaps(CanvasRectangle other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return CanvasIntersection.Overlaps(X, Y, Width, Height, other.X, other.Y, other.Width, other.Height);
    }
}

public sealed class PlotLayout
{
    public ImmutableArray<CanvasRectangle> Obstacles { get; }
    public PlotLayout(IEnumerable<CanvasRectangle> obstacles)
    {
        ArgumentNullException.ThrowIfNull(obstacles);
        Obstacles = obstacles.ToImmutableArray();
    }

    public bool CollidesWith(CanvasRectangle rectangle) => Obstacles.Any(rectangle.Overlaps);

    public Result<Option<CanvasRectangle>> FindFreeSpot(double width, double height, double canvasWidth, double canvasHeight)
        => CanvasRectangle.Create(0, 0, width, height).Bind(shape =>
            CanvasRectangle.Create(0, 0, canvasWidth, canvasHeight).Bind(canvas => Result.Success(FindSpace(shape, canvas))));

    private Option<CanvasRectangle> FindSpace(CanvasRectangle shape, CanvasRectangle canvas)
    {
        var xs = new SortedSet<double> { 0 };
        var ys = new SortedSet<double> { 0 };
        foreach (var obstacle in Obstacles)
        {
            xs.Add(obstacle.Right);
            if (obstacle.X - shape.Width >= 0) xs.Add(obstacle.X - shape.Width);
            ys.Add(obstacle.Bottom);
            if (obstacle.Y - shape.Height >= 0) ys.Add(obstacle.Y - shape.Height);
        }

        foreach (var y in ys)
        foreach (var x in xs)
        {
            if (x < 0 || y < 0 || x + shape.Width > canvas.Width || y + shape.Height > canvas.Height) continue;
            var candidate = CanvasRectangle.Create(x, y, shape.Width, shape.Height).ValueOrThrow();
            if (!CollidesWith(candidate)) return Option.Some(candidate);
        }
        return Option.None<CanvasRectangle>();
    }
}
