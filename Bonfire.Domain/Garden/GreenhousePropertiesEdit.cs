using System.Collections.Immutable;
using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Garden;

public sealed record GreenhouseEditorValues(string Name, Option<string> Note, Option<string> Material, double WidthMeters, double HeightMeters)
{
    public GreenhouseEditorValues WithChangesFrom(GreenhouseEditorValues original, GreenhouseEditorValues edited)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(edited);
        return new(Name.WithChangeFrom(original.Name, edited.Name), Note.WithChangeFrom(original.Note, edited.Note),
            Material.WithChangeFrom(original.Material, edited.Material),
            WidthMeters.WithChangeFrom(original.WidthMeters, edited.WidthMeters), HeightMeters.WithChangeFrom(original.HeightMeters, edited.HeightMeters));
    }
}

public sealed record InteriorElement(string Name, CanvasRectangle Rectangle);
public sealed class GreenhouseContents
{
    public ImmutableArray<InteriorElement> Elements { get; }
    public GreenhouseContents(IEnumerable<InteriorElement> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        Elements = elements.ToImmutableArray();
    }

    public Result<CanvasRectangle> RequireFit(CanvasRectangle canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        foreach (var element in Elements)
            if (element.Rectangle.Right > canvas.Width || element.Rectangle.Bottom > canvas.Height)
                return Result.Failure<CanvasRectangle>($"Нельзя уменьшить теплицу: элемент «{element.Name}» " +
                    $"(правый/нижний край {element.Rectangle.Right:F0}×{element.Rectangle.Bottom:F0} пкс) " +
                    $"выходит за новые границы {canvas.Width:F0}×{canvas.Height:F0} пкс.");
        return Result.Success(canvas);
    }
}

public sealed record GreenhousePropertiesState(int Id, GreenhouseEditorValues Values, CanvasRectangle Canvas);

public sealed class GreenhousePropertiesEdit
{
    public int Id { get; }
    public GreenhouseEditorValues Original { get; }
    public GreenhouseEditorValues Values { get; }
    public double PixelsPerMeter { get; }
    public bool HasSizeChanges => (Original.WidthMeters, Original.HeightMeters) != (Values.WidthMeters, Values.HeightMeters);
    private GreenhousePropertiesEdit(int id, GreenhouseEditorValues original, GreenhouseEditorValues values, double pixelsPerMeter)
        => (Id, Original, Values, PixelsPerMeter) = (id, original, values, pixelsPerMeter);

    public static Result<GreenhousePropertiesEdit> Create(int id, GreenhouseEditorValues original, GreenhouseEditorValues values, double pixelsPerMeter)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(values.Name);
        if (id <= 0) return Result.Failure<GreenhousePropertiesEdit>("Идентификатор должен быть положительным");
        return PlotDimensions.Create(original.WidthMeters, original.HeightMeters, pixelsPerMeter).Bind(_ =>
            PlotDimensions.Create(values.WidthMeters, values.HeightMeters, pixelsPerMeter)
                .Bind(_ => Result.Success(new GreenhousePropertiesEdit(id, original, values, pixelsPerMeter))));
    }

    public Result<GreenhousePropertiesState> ApplyTo(GreenhousePropertiesState current, GreenhouseContents contents)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(contents);
        if (current.Id != Id) return Result.Failure<GreenhousePropertiesState>("Редактор относится к другой теплице");
        var merged = current.Values.WithChangesFrom(Original, Values);
        var canvas = PlotDimensions.Create(merged.WidthMeters, merged.HeightMeters, PixelsPerMeter)
            .Bind(size => Result.Success(HasSizeChanges ? size.Canvas : current.Canvas));
        return canvas.Bind(contents.RequireFit).Bind(bounds => Result.Success(current with { Values = merged, Canvas = bounds }));
    }
}
