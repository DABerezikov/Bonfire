using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planting;

public sealed class DbElementPropertiesWriter(IUnitOfWorkFactory units) : IElementPropertiesWriter
{
    public async Task<Result<ElementPropertiesResultFromViewModel>> ApplyAsync(ElementPropertiesEdit edit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(edit);
        cancellationToken.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        var element = await unit.Repository<GardenElement>().GetAsync(edit.Id, cancellationToken).ConfigureAwait(false);
        if (element is null) return Result.Failure<ElementPropertiesResultFromViewModel>("Элемент уже удалён");
        var layout = edit.HasGeometryChanges
            ? await DbCanvasSiblings.ForElementAsync(unit, element.PlotId, element.Id, cancellationToken)
                .ConfigureAwait(false)
            : Result.Success(new PlotLayout([]));
        var updated = ElementPropertiesDbMapper.Read(element)
            .Bind(current => layout.Bind(siblings => edit.ApplyTo(current, siblings)));
        return await updated.Match(async state =>
        {
            ElementPropertiesDbMapper.Apply(state, element);
            try
            {
                await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ElementPropertiesDbMapper.Receipt(element);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<ElementPropertiesResultFromViewModel>("Запись изменена другим процессом");
            }
            catch (DbUpdateException error)
            {
                return Result.Failure<ElementPropertiesResultFromViewModel>(
                    $"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
            }
        }, error => Task.FromResult(Result.Failure<ElementPropertiesResultFromViewModel>(error))).ConfigureAwait(false);
    }
}