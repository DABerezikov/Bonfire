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
        new(Details.Name.WithChangeFrom(original.Details.Name, edited.Details.Name),
            Details.Note.WithChangeFrom(original.Details.Note, edited.Details.Note),
            Details.SoilType.WithChangeFrom(original.Details.SoilType, edited.Details.SoilType),
            Details.Orientation.WithChangeFrom(original.Details.Orientation, edited.Details.Orientation),
            Details.CoverMaterial.WithChangeFrom(original.Details.CoverMaterial, edited.Details.CoverMaterial)),
        X.WithChangeFrom(original.X, edited.X), Y.WithChangeFrom(original.Y, edited.Y),
        Width.WithChangeFrom(original.Width, edited.Width), Height.WithChangeFrom(original.Height, edited.Height));
    }

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
