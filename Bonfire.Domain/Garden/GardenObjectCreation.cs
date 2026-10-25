using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Garden;

public abstract record GardenElementKind
{
    private GardenElementKind() { }
    public sealed record Bed : GardenElementKind;
    public sealed record ColdFrame : GardenElementKind;
    public sealed record FlowerBed : GardenElementKind;
    public sealed record OpenGroundArea : GardenElementKind;
}

public sealed record GardenElementTemplate(GardenElementKind Kind, string Label, double WidthMeters, double HeightMeters)
{
    public static GardenElementTemplate From(string code) => code switch
    {
        "Bed" => new(new GardenElementKind.Bed(), "Грядка", 2, 0.9),
        "ColdFrame" => new(new GardenElementKind.ColdFrame(), "Парник", 1.2, 0.8),
        "FlowerBed" => new(new GardenElementKind.FlowerBed(), "Цветник", 0.6, 0.6),
        "OpenGroundArea" => new(new GardenElementKind.OpenGroundArea(), "Открытый грунт", 2, 2),
        _ => new(new GardenElementKind.Bed(), "Элемент", 1, 1)
    };
}

public abstract record GardenObjectCreation
{
    public int PlotId { get; }
    public string Name { get; }
    public CanvasRectangle Rectangle { get; }
    private GardenObjectCreation(int plotId, string name, CanvasRectangle rectangle)
        => (PlotId, Name, Rectangle) = (plotId, name, rectangle);

    public sealed record Element : GardenObjectCreation
    {
        public GardenElementKind Kind { get; }
        internal Element(int plotId, string name, CanvasRectangle rectangle, GardenElementKind kind) : base(plotId, name, rectangle) => Kind = kind;
    }

    public sealed record Greenhouse : GardenObjectCreation
    {
        public PlotDimensions Dimensions { get; }
        internal Greenhouse(int plotId, string name, CanvasRectangle rectangle, PlotDimensions dimensions) : base(plotId, name, rectangle) => Dimensions = dimensions;
    }

    public static Result<Element> CreateElement(int plotId, string name, GardenElementKind kind, double x, double y, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(name);
        return plotId <= 0 ? Result.Failure<Element>("Идентификатор контейнера должен быть положительным")
            : CanvasRectangle.Create(x, y, width, height).Bind(rectangle => Result.Success(new Element(plotId, name, rectangle, kind)));
    }

    public static Result<Greenhouse> CreateGreenhouse(int plotId, string name, double widthMeters, double heightMeters, double scale, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(name);
        return plotId <= 0 ? Result.Failure<Greenhouse>("Идентификатор контейнера должен быть положительным")
            : PlotDimensions.Create(widthMeters, heightMeters, scale).Bind(dimensions =>
                CanvasRectangle.Create(x, y, dimensions.Canvas.Width, dimensions.Canvas.Height)
                    .Bind(rectangle => Result.Success(new Greenhouse(plotId, name, rectangle, dimensions))));
    }

    public Result<CanvasRectangle> RequirePlacement(CanvasRectangle container, PlotLayout siblings)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(siblings);
        if (Rectangle.X < 0 || Rectangle.Y < 0 || Rectangle.Right > container.Width || Rectangle.Bottom > container.Height)
            return Result.Failure<CanvasRectangle>("Новое размещение выходит за границы контейнера");
        return siblings.CollidesWith(Rectangle)
            ? Result.Failure<CanvasRectangle>("Новое размещение пересекается с соседним объектом")
            : Result.Success(Rectangle);
    }
}
