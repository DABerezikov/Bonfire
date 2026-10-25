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
        var layout = await ReadLayoutAsync(unit, element.PlotId, edit, token).ConfigureAwait(false);
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
        var layout = await ReadLayoutAsync(unit, greenhouse.ParentPlotId, edit, token).ConfigureAwait(false);
        return await original.Bind(frame => layout.Bind(siblings => edit.ValidateAgainst(frame, greenhouse.IsLocked, siblings))).Match(_ =>
        {
            (greenhouse.X, greenhouse.Y, greenhouse.DisplayWidth, greenhouse.DisplayHeight) = (edit.Rectangle.X, edit.Rectangle.Y, edit.Rectangle.Width, edit.Rectangle.Height);
            return SaveAsync(unit, edit, token);
        }, error => Task.FromResult(Result.Failure<GeometryResultFromViewModel>(error))).ConfigureAwait(false);
    }

    private static async Task<Result<PlotLayout>> ReadLayoutAsync(IUnitOfWork unit, int plotId, GardenGeometryEdit edit, CancellationToken token)
    {
        var elements = unit.Repository<GardenElement>().Items.Where(element => element.PlotId == plotId);
        var greenhouses = unit.Repository<Greenhouse>().Items.Where(greenhouse => greenhouse.ParentPlotId == plotId);
        switch (edit)
        {
            case GardenGeometryEdit.Element: elements = elements.Where(element => element.Id != edit.Id); break;
            case GardenGeometryEdit.Greenhouse: greenhouses = greenhouses.Where(greenhouse => greenhouse.Id != edit.Id); break;
        }
        var elementFrames = await elements.Select(element => new { element.X, element.Y, Width = element.DisplayWidth, Height = element.DisplayHeight }).ToArrayAsync(token).ConfigureAwait(false);
        var greenhouseFrames = await greenhouses.Select(greenhouse => new { greenhouse.X, greenhouse.Y, Width = greenhouse.DisplayWidth, Height = greenhouse.DisplayHeight }).ToArrayAsync(token).ConfigureAwait(false);
        return PlotLayout.Create(elementFrames.Concat(greenhouseFrames).Select(frame => CanvasRectangle.Create(frame.X, frame.Y, frame.Width, frame.Height)));
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
