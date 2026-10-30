using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planting;

public sealed class DbGardenObjectCreationWriter(IUnitOfWorkFactory units) : IGardenObjectCreationWriter
{
    public Task<Result<ElementCreationResultFromViewModel>> CreateElementAsync(GardenObjectCreation.Element creation, CancellationToken cancellationToken)
        => CreateAsync<GardenElement, ElementCreationResultFromViewModel>(creation, () =>
        {
            GardenElement row = creation.Kind switch
            {
                GardenElementKind.Bed => new Bed(),
                GardenElementKind.ColdFrame => new ColdFrame(),
                GardenElementKind.FlowerBed => new FlowerBed(),
                GardenElementKind.OpenGroundArea => new OpenGroundArea(),
                _ => throw new System.Diagnostics.UnreachableException()
            };
            row.PlotId = creation.PlotId; row.Name = creation.Name;
            row.X = creation.Rectangle.X; row.Y = creation.Rectangle.Y;
            row.DisplayWidth = creation.Rectangle.Width; row.DisplayHeight = creation.Rectangle.Height;
            return row;
        }, (row, container) => ElementPropertiesDbMapper.Receipt(row)
            .Bind(properties => Result.Success(new ElementCreationResultFromViewModel(properties, row.GetType().Name, container))), cancellationToken);

    public Task<Result<GreenhouseCreationResultFromViewModel>> CreateGreenhouseAsync(GardenObjectCreation.Greenhouse creation, CancellationToken cancellationToken)
        => CreateAsync<Greenhouse, GreenhouseCreationResultFromViewModel>(creation, () => new Greenhouse
        {
            ParentPlotId = creation.PlotId, Name = creation.Name,
            X = creation.Rectangle.X, Y = creation.Rectangle.Y,
            DisplayWidth = creation.Rectangle.Width, DisplayHeight = creation.Rectangle.Height,
            WidthMeters = creation.Dimensions.WidthMeters, HeightMeters = creation.Dimensions.HeightMeters,
            CanvasWidth = creation.Dimensions.Canvas.Width, CanvasHeight = creation.Dimensions.Canvas.Height
        }, (row, container) => GreenhousePropertiesDbMapper.Read(row)
            .Bind(state => Result.Success(new GreenhouseCreationResultFromViewModel(GreenhousePropertiesDbMapper.Receipt(state), creation.Rectangle, container))), cancellationToken);

    private async Task<Result<TResult>> CreateAsync<TEntity, TResult>(GardenObjectCreation creation, Func<TEntity> create,
        Func<TEntity, CanvasRectangle, Result<TResult>> receipt, CancellationToken token) where TEntity : Entity
    {
        ArgumentNullException.ThrowIfNull(creation);
        token.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        var gardens = unit.Repository<Garden>().Items.Where(g => g.Id == creation.PlotId)
            .Select(g => new { g.CanvasWidth, g.CanvasHeight });
        var houses = unit.Repository<Greenhouse>().Items.Where(h => h.Id == creation.PlotId)
            .Select(h => new { h.CanvasWidth, h.CanvasHeight });
        var current = await gardens.Concat(houses).SingleOrDefaultAsync(token).ConfigureAwait(false);
        if (current is null) return Result.Failure<TResult>("Контейнер уже удалён");
        var bounds = CanvasRectangle.Create(0, 0, current.CanvasWidth, current.CanvasHeight);
        var layout = await DbCanvasSiblings.ForPlotAsync(unit, creation.PlotId, token).ConfigureAwait(false);
        var placement = bounds.Bind(container => layout.Bind(siblings => creation.RequirePlacement(container, siblings)));
        return await placement.Match(async _ =>
        {
            var row = create();
            try
            {
                await unit.Repository<TEntity>().AddAsync(row, token).ConfigureAwait(false);
                await unit.SaveChangesAsync(token).ConfigureAwait(false);
                return receipt(row, bounds.ValueOrThrow());
            }
            catch (DbUpdateException error)
            {
                return Result.Failure<TResult>($"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
            }
        }, error => Task.FromResult(Result.Failure<TResult>(error))).ConfigureAwait(false);
    }
}
