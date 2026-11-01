using System;
using System.Diagnostics;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using Bonfire.Models;
using Bonfire.Models.Mappers;

namespace Bonfire.Infrastructure;

internal static class CanvasGestureAdapter
{
    public static Option<CanvasRectangle> Capture(GardenElementFromViewModel model) =>
        Capture(CanvasGeometryMapper.Read(model));

    public static Option<CanvasRectangle> Capture(GreenhouseFromViewModel model) =>
        Capture(CanvasGeometryMapper.Read(model));

    public static Result<CanvasRectangle> Move(GardenElementFromViewModel model, double x, double y, double scale)
        => Move(CanvasGeometryMapper.Read(model), model.ContainerCanvasWidth, model.ContainerCanvasHeight, x, y, scale);

    public static Result<CanvasRectangle> Move(GreenhouseFromViewModel model, double x, double y, double scale)
        => Move(CanvasGeometryMapper.Read(model), model.ContainerCanvasWidth, model.ContainerCanvasHeight, x, y, scale);

    public static Result<CanvasRectangle> Resize(GardenElementFromViewModel model, double x, double y, double scale)
        => Resize(CanvasGeometryMapper.Read(model), model.ContainerCanvasWidth, model.ContainerCanvasHeight, x, y,
            scale, 15);

    public static Result<CanvasRectangle> Resize(GreenhouseFromViewModel model, double x, double y, double scale)
        => Resize(CanvasGeometryMapper.Read(model), model.ContainerCanvasWidth, model.ContainerCanvasHeight, x, y,
            scale, 150);

    public static Result<Option<CanvasRectangle>> CompleteMove(GardenElementFromViewModel model,
        Option<CanvasRectangle> start)
        => Complete(CanvasGeometryMapper.Read(model), start, MoveOrigin,
            rectangle =>
                CollisionHelper.CollidesWithSiblings(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height,
                    model));

    public static Result<Option<CanvasRectangle>> CompleteMove(GreenhouseFromViewModel model,
        Option<CanvasRectangle> start)
        => Complete(CanvasGeometryMapper.Read(model), start, MoveOrigin,
            rectangle =>
                CollisionHelper.CollidesWithSiblings(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height,
                    model));

    public static Result<Option<CanvasRectangle>> CompleteResize(GardenElementFromViewModel model,
        Option<CanvasRectangle> start)
        => Complete(CanvasGeometryMapper.Read(model), start, ResizeOrigin,
            rectangle =>
                CollisionHelper.CollidesWithSiblings(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height,
                    model));

    public static Result<Option<CanvasRectangle>> CompleteResize(GreenhouseFromViewModel model,
        Option<CanvasRectangle> start)
        => Complete(CanvasGeometryMapper.Read(model), start, ResizeOrigin,
            rectangle =>
                CollisionHelper.CollidesWithSiblings(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height,
                    model));

    public static void Apply(Result<CanvasRectangle> result, Action<CanvasRectangle> update)
        => result.Match(rectangle =>
        {
            update(rectangle);
            return true;
        }, ReportFailure);

    public static void Apply(Result<Option<CanvasRectangle>> result, Action<CanvasRectangle> update)
        => result.Match(optional => optional.Match(rectangle =>
        {
            update(rectangle);
            return true;
        }, () => false), ReportFailure);

    private static Option<CanvasRectangle> Capture(Result<CanvasRectangle> result)
        => result.Match(Option.Some, error =>
        {
            ReportFailure(error);
            return Option.None<CanvasRectangle>();
        });

    private static Result<CanvasRectangle> Move(Result<CanvasRectangle> current, double width, double height, double x,
        double y, double scale)
        => current.Bind(rectangle => CanvasGeometryMapper.ReadBounds(width, height)
            .Bind(bounds => rectangle.WithMoved(x, y, scale, bounds)));

    private static Result<CanvasRectangle> Resize(Result<CanvasRectangle> current, double width, double height,
        double x, double y, double scale, double minimum)
        => current.Bind(rectangle => CanvasGeometryMapper.ReadBounds(width, height)
            .Bind(bounds => rectangle.WithResized(x, y, scale, bounds, minimum)));

    private static Result<CanvasRectangle> MoveOrigin(CanvasRectangle start, CanvasRectangle current)
        => CanvasRectangle.Create(start.X, start.Y, current.Width, current.Height);

    private static Result<CanvasRectangle> ResizeOrigin(CanvasRectangle start, CanvasRectangle current)
        => CanvasRectangle.Create(current.X, current.Y, start.Width, start.Height);

    private static Result<Option<CanvasRectangle>> Complete(Result<CanvasRectangle> current,
        Option<CanvasRectangle> start,
        Func<CanvasRectangle, CanvasRectangle, Result<CanvasRectangle>> origin,
        Func<CanvasRectangle, bool> collides)
        => start.Match(before => current.Bind(rectangle => origin(before, rectangle)
                .Bind(original =>
                {
                    var resolved = rectangle.ResolveDropFrom(original, collides);
                    return Result.Success(ReferenceEquals(resolved, rectangle)
                        ? Option.None<CanvasRectangle>()
                        : Option.Some(resolved));
                })),
            () => Result.Success(Option.None<CanvasRectangle>()));

    private static bool ReportFailure(string error)
    {
        Trace.TraceWarning("Геометрия жеста не применена: {0}", error);
        return false;
    }
}