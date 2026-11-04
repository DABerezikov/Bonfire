using System.Diagnostics;
using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Garden;

public abstract record GeometryTarget
{
    private GeometryTarget()
    {
    }

    public sealed record Element(double PixelsPerMeter) : GeometryTarget;

    public sealed record Greenhouse : GeometryTarget;
}

public abstract record GardenGeometryEdit
{
    private GardenGeometryEdit(int id, CanvasRectangle rectangle) => (Id, Rectangle) = (id, rectangle);
    public int Id { get; }
    public CanvasRectangle Rectangle { get; }

    public static Result<GardenGeometryEdit> Create(int id, GeometryTarget target, double x, double y, double width,
        double height)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (id <= 0) return Result.Failure<GardenGeometryEdit>("Идентификатор должен быть положительным");
        return CanvasRectangle.Create(x, y, width, height).Bind(rectangle => target switch
        {
            GeometryTarget.Element element => rectangle.MeasureArea(element.PixelsPerMeter)
                .Bind(area => Result.Success<GardenGeometryEdit>(new Element(id, rectangle, area))),
            GeometryTarget.Greenhouse => Result.Success<GardenGeometryEdit>(new Greenhouse(id, rectangle)),
            _ => throw new UnreachableException()
        });
    }

    public Result<GardenGeometryEdit> ValidateAgainst(CanvasRectangle original, bool isLocked, PlotLayout siblings)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(siblings);
        if (isLocked) return Result.Failure<GardenGeometryEdit>("Геометрия заблокирована");
        return ReferenceEquals(Rectangle.ResolveDropFrom(original, siblings.CollidesWith), Rectangle)
            ? Result.Success(this)
            : Result.Failure<GardenGeometryEdit>("Новое размещение пересекается с соседним объектом");
    }

    public sealed record Element : GardenGeometryEdit
    {
        internal Element(int id, CanvasRectangle rectangle, double area) : base(id, rectangle) =>
            AreaSquareMeters = area;

        public double AreaSquareMeters { get; }
    }

    public sealed record Greenhouse : GardenGeometryEdit
    {
        internal Greenhouse(int id, CanvasRectangle rectangle) : base(id, rectangle)
        {
        }
    }
}