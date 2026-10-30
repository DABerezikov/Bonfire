using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planting;

public sealed class DbGridWriter(IUnitOfWorkFactory units) : IGridWriter
{
    public async Task<Result<GridResultFromViewModel>> ApplyAsync(int elementId, GridSize size, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(size);
        cancellationToken.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        var element = await unit.Repository<GardenElement>().GetAsync(elementId, cancellationToken).ConfigureAwait(false);
        if (element is null) return Result.Failure<GridResultFromViewModel>("Элемент уже удалён");
        return await DbGridMapper.Read(element).WithSize(size, element.State).Match(async change =>
        {
            try
            {
                await DbGridMapper.ApplyAsync(change, element, unit, cancellationToken).ConfigureAwait(false);
                await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return Result.Success(DbGridMapper.Receipt(element));
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<GridResultFromViewModel>("Запись изменена другим процессом");
            }
            catch (DbUpdateException error)
            {
                return Result.Failure<GridResultFromViewModel>($"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
            }
        }, error => Task.FromResult(Result.Failure<GridResultFromViewModel>(error))).ConfigureAwait(false);
    }
}
