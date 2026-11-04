using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planting;

internal static class DbCanvasSiblings
{
    public static Task<Result<PlotLayout>> ForPlotAsync(IUnitOfWork unit, int plotId, CancellationToken token)
        => ReadAsync(unit, plotId, Option.None<int>(), Option.None<int>(), token);

    public static Task<Result<PlotLayout>> ForElementAsync(IUnitOfWork unit, int plotId, int id,
        CancellationToken token)
        => ReadAsync(unit, plotId, Option.Some(id), Option.None<int>(), token);

    public static Task<Result<PlotLayout>> ForGreenhouseAsync(IUnitOfWork unit, int plotId, int id,
        CancellationToken token)
        => ReadAsync(unit, plotId, Option.None<int>(), Option.Some(id), token);

    private static async Task<Result<PlotLayout>> ReadAsync(IUnitOfWork unit, int plotId, Option<int> excludedElement,
        Option<int> excludedGreenhouse, CancellationToken token)
    {
        var elements = unit.Repository<GardenElement>().Items.Where(element => element.PlotId == plotId);
        var greenhouses = unit.Repository<Greenhouse>().Items.Where(greenhouse => greenhouse.ParentPlotId == plotId);
        elements = excludedElement.Match(id => elements.Where(element => element.Id != id), () => elements);
        greenhouses = excludedGreenhouse.Match(id => greenhouses.Where(greenhouse => greenhouse.Id != id),
            () => greenhouses);
        var elementFrames = await elements
            .Select(element => new
                { element.X, element.Y, Width = element.DisplayWidth, Height = element.DisplayHeight })
            .ToArrayAsync(token).ConfigureAwait(false);
        var greenhouseFrames = await greenhouses
            .Select(greenhouse => new
                { greenhouse.X, greenhouse.Y, Width = greenhouse.DisplayWidth, Height = greenhouse.DisplayHeight })
            .ToArrayAsync(token).ConfigureAwait(false);
        return PlotLayout.Create(elementFrames.Concat(greenhouseFrames)
            .Select(frame => CanvasRectangle.Create(frame.X, frame.Y, frame.Width, frame.Height)));
    }
}