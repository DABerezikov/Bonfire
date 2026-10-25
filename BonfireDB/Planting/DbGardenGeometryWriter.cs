using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planting;

public sealed class DbGardenGeometryWriter(IUnitOfWorkFactory units) : IGardenGeometryWriter
{
    public async Task<Result<GeometryResultFromViewModel>> ApplyAsync(GardenGeometryEdit edit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(edit);
        cancellationToken.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        return await (edit switch
        {
            GardenGeometryEdit.Element element => SaveElementAsync(unit, element, cancellationToken),
            GardenGeometryEdit.Greenhouse greenhouse => SaveGreenhouseAsync(unit, greenhouse, cancellationToken),
            _ => throw new System.Diagnostics.UnreachableException()
        }).ConfigureAwait(false);
    }

    private static async Task<Result<GeometryResultFromViewModel>> SaveElementAsync(IUnitOfWork unit, GardenGeometryEdit.Element edit, CancellationToken token)
    {
        var element = await unit.Repository<GardenElement>().GetAsync(edit.Id, token).ConfigureAwait(false);
        if (element is null) return Result.Failure<GeometryResultFromViewModel>("Элемент уже удалён");
        var original = CanvasRectangle.Create(element.X, element.Y, element.DisplayWidth, element.DisplayHeight);
        var layout = await DbCanvasSiblings.ForElementAsync(unit, element.PlotId, edit.Id, token).ConfigureAwait(false);
        return await original.Bind(frame => layout.Bind(siblings => edit.ValidateAgainst(frame, element.IsLocked, siblings))).Match(_ =>
        {
            (element.X, element.Y, element.DisplayWidth, element.DisplayHeight) = (edit.Rectangle.X, edit.Rectangle.Y, edit.Rectangle.Width, edit.Rectangle.Height);
            element.AreaSquareMeters = edit.AreaSquareMeters;
            return SaveAsync(unit, edit, token);
        }, error => Task.FromResult(Result.Failure<GeometryResultFromViewModel>(error))).ConfigureAwait(false);
    }

    private static async Task<Result<GeometryResultFromViewModel>> SaveGreenhouseAsync(IUnitOfWork unit, GardenGeometryEdit.Greenhouse edit, CancellationToken token)
    {
        var greenhouse = await unit.Repository<Greenhouse>().GetAsync(edit.Id, token).ConfigureAwait(false);
        if (greenhouse is null) return Result.Failure<GeometryResultFromViewModel>("Теплица уже удалена");
        var original = CanvasRectangle.Create(greenhouse.X, greenhouse.Y, greenhouse.DisplayWidth, greenhouse.DisplayHeight);
        var layout = await DbCanvasSiblings.ForGreenhouseAsync(unit, greenhouse.ParentPlotId, edit.Id, token).ConfigureAwait(false);
        return await original.Bind(frame => layout.Bind(siblings => edit.ValidateAgainst(frame, greenhouse.IsLocked, siblings))).Match(_ =>
        {
            (greenhouse.X, greenhouse.Y, greenhouse.DisplayWidth, greenhouse.DisplayHeight) = (edit.Rectangle.X, edit.Rectangle.Y, edit.Rectangle.Width, edit.Rectangle.Height);
            return SaveAsync(unit, edit, token);
        }, error => Task.FromResult(Result.Failure<GeometryResultFromViewModel>(error))).ConfigureAwait(false);
    }

    private static async Task<Result<GeometryResultFromViewModel>> SaveAsync(IUnitOfWork unit, GardenGeometryEdit edit, CancellationToken token)
    {
        try
        {
            await unit.SaveChangesAsync(token).ConfigureAwait(false);
            return Result.Success(new GeometryResultFromViewModel(edit.Id, edit.Rectangle));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<GeometryResultFromViewModel>("Запись изменена другим процессом");
        }
        catch (DbUpdateException error)
        {
            return Result.Failure<GeometryResultFromViewModel>($"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
        }
    }
}
