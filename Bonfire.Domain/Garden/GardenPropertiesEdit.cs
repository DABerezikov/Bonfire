using System.Collections.Immutable;
using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Garden;

public sealed record GardenEditorValues(
    string Name,
    Option<string> Note,
    Option<string> Address,
    double WidthMeters,
    double HeightMeters)
{
    public GardenEditorValues WithChangesFrom(GardenEditorValues original, GardenEditorValues edited)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(edited);
        return new GardenEditorValues(Name.WithChangeFrom(original.Name, edited.Name),
            Note.WithChangeFrom(original.Note, edited.Note),
            Address.WithChangeFrom(original.Address, edited.Address),
            WidthMeters.WithChangeFrom(original.WidthMeters, edited.WidthMeters),
            HeightMeters.WithChangeFrom(original.HeightMeters, edited.HeightMeters));
    }
}

public sealed class GardenContents
{
    public GardenContents(IEnumerable<InteriorElement> elements, IEnumerable<InteriorElement> greenhouses)
    {
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(greenhouses);
        Elements = elements.ToImmutableArray();
        Greenhouses = greenhouses.ToImmutableArray();
    }

    public ImmutableArray<InteriorElement> Elements { get; }
    public ImmutableArray<InteriorElement> Greenhouses { get; }

    public Result<CanvasRectangle> RequireFit(CanvasRectangle canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        foreach (var element in Elements)
            if (element.Rectangle.Right > canvas.Width || element.Rectangle.Bottom > canvas.Height)
                return Result.Failure<CanvasRectangle>($"Нельзя уменьшить участок: элемент «{element.Name}» " +
                                                       $"(правый/нижний край {element.Rectangle.Right:F0}×{element.Rectangle.Bottom:F0} пкс) " +
                                                       $"выходит за новые границы {canvas.Width:F0}×{canvas.Height:F0} пкс.");
        foreach (var house in Greenhouses)
            if (house.Rectangle.Right > canvas.Width || house.Rectangle.Bottom > canvas.Height)
                return Result.Failure<CanvasRectangle>(
                    $"Нельзя уменьшить участок: теплица «{house.Name}» выходит за новые границы.");
        return Result.Success(canvas);
    }
}

public sealed record GardenPropertiesState(int Id, GardenEditorValues Values, CanvasRectangle Canvas);

public sealed class GardenPropertiesEdit
{
    private GardenPropertiesEdit(int id, GardenEditorValues original, GardenEditorValues values, double pixelsPerMeter)
        => (Id, Original, Values, PixelsPerMeter) = (id, original, values, pixelsPerMeter);

    public int Id { get; }
    public GardenEditorValues Original { get; }
    public GardenEditorValues Values { get; }
    public double PixelsPerMeter { get; }

    public bool HasSizeChanges =>
        (Original.WidthMeters, Original.HeightMeters) != (Values.WidthMeters, Values.HeightMeters);

    public static Result<GardenPropertiesEdit> Create(int id, GardenEditorValues original, GardenEditorValues values,
        double pixelsPerMeter)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(values.Name);
        if (id <= 0) return Result.Failure<GardenPropertiesEdit>("Идентификатор должен быть положительным");
        return PlotDimensions.Create(original.WidthMeters, original.HeightMeters, pixelsPerMeter).Bind(_ =>
            PlotDimensions.Create(values.WidthMeters, values.HeightMeters, pixelsPerMeter)
                .Bind(_ => Result.Success(new GardenPropertiesEdit(id, original, values, pixelsPerMeter))));
    }

    public Result<GardenPropertiesState> ApplyTo(GardenPropertiesState current, GardenContents contents)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(contents);
        if (current.Id != Id) return Result.Failure<GardenPropertiesState>("Редактор относится к другому участку");
        var merged = current.Values.WithChangesFrom(Original, Values);
        var canvas = PlotDimensions.Create(merged.WidthMeters, merged.HeightMeters, PixelsPerMeter)
            .Bind(size => Result.Success(HasSizeChanges ? size.Canvas : current.Canvas));
        return canvas.Bind(contents.RequireFit)
            .Bind(bounds => Result.Success(current with { Values = merged, Canvas = bounds }));
    }
}