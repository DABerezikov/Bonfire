using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Garden;

public sealed record ElementDetailsValues(string Name, Option<string> Note, Option<string> SoilType,
    Option<string> Orientation, Option<string> CoverMaterial);
public sealed record ElementEditorValues(ElementDetailsValues Details, double X, double Y, double Width, double Height)
{
    public ElementEditorValues WithChangesFrom(ElementEditorValues original, ElementEditorValues edited)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(edited);
        return new(
        new(Pick(original.Details.Name, edited.Details.Name, Details.Name),
            Pick(original.Details.Note, edited.Details.Note, Details.Note),
            Pick(original.Details.SoilType, edited.Details.SoilType, Details.SoilType),
            Pick(original.Details.Orientation, edited.Details.Orientation, Details.Orientation),
            Pick(original.Details.CoverMaterial, edited.Details.CoverMaterial, Details.CoverMaterial)),
        Pick(original.X, edited.X, X), Pick(original.Y, edited.Y, Y),
        Pick(original.Width, edited.Width, Width), Pick(original.Height, edited.Height, Height));
    }

    private static T Pick<T>(T original, T edited, T current)
        => EqualityComparer<T>.Default.Equals(original, edited) ? current : edited;
}
public sealed record ElementPropertiesState(int Id, ElementDetailsValues Details, CanvasRectangle Rectangle, double AreaSquareMeters, bool IsLocked);

public sealed class ElementPropertiesEdit
{
    public int Id { get; }
    public ElementEditorValues Original { get; }
    public ElementEditorValues Values { get; }
    public double PixelsPerMeter { get; }
    public bool HasGeometryChanges => (Original.X, Original.Y, Original.Width, Original.Height) != (Values.X, Values.Y, Values.Width, Values.Height);

    private ElementPropertiesEdit(int id, ElementEditorValues original, ElementEditorValues values, double pixelsPerMeter)
        => (Id, Original, Values, PixelsPerMeter) = (id, original, values, pixelsPerMeter);

    public static Result<ElementPropertiesEdit> Create(int id, ElementEditorValues original, ElementEditorValues values, double pixelsPerMeter)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(original.Details);
        ArgumentNullException.ThrowIfNull(values.Details);
        ArgumentNullException.ThrowIfNull(values.Details.Name);
        if (id <= 0) return Result.Failure<ElementPropertiesEdit>("Идентификатор должен быть положительным");
        return CanvasRectangle.Create(original.X, original.Y, original.Width, original.Height).Bind(_ =>
            CanvasRectangle.Create(values.X, values.Y, values.Width, values.Height).Bind(rectangle =>
                rectangle.MeasureArea(pixelsPerMeter).Bind(_ => Result.Success(new ElementPropertiesEdit(id, original, values, pixelsPerMeter)))));
    }

    public Result<ElementPropertiesState> ApplyTo(ElementPropertiesState current, PlotLayout siblings)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(siblings);
        if (current.Id != Id) return Result.Failure<ElementPropertiesState>("Редактор относится к другому элементу");
        var merged = new ElementEditorValues(current.Details, current.Rectangle.X, current.Rectangle.Y, current.Rectangle.Width, current.Rectangle.Height)
            .WithChangesFrom(Original, Values);
        if (!HasGeometryChanges) return Result.Success(current with { Details = merged.Details });

        return GardenGeometryEdit.Create(Id, new GeometryTarget.Element(PixelsPerMeter),
            merged.X, merged.Y, merged.Width, merged.Height)
            .Bind(edit => edit.ValidateAgainst(current.Rectangle, current.IsLocked, siblings))
            .Bind(edit => Result.Success(current with
            {
                Details = merged.Details, Rectangle = edit.Rectangle,
                AreaSquareMeters = ((GardenGeometryEdit.Element)edit).AreaSquareMeters
            }));
    }
}
