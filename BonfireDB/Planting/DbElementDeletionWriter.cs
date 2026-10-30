using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planting;

public sealed class DbElementDeletionWriter(IUnitOfWorkFactory units) : IElementDeletionWriter
{
    public async Task<Result<ElementDeletionResultFromViewModel>> DeleteAsync(GardenElementId id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);
        cancellationToken.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        var repository = unit.Repository<GardenElement>();
        var element = await repository.GetAsync(id.Value, cancellationToken).ConfigureAwait(false);
        if (element is null) return Result.Failure<ElementDeletionResultFromViewModel>("Элемент уже удалён");
        try
        {
            await repository.RemoveAsync(id.Value, cancellationToken).ConfigureAwait(false);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(new ElementDeletionResultFromViewModel(id.Value));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<ElementDeletionResultFromViewModel>("Запись изменена другим процессом");
        }
        catch (DbUpdateException error)
        {
            return Result.Failure<ElementDeletionResultFromViewModel>($"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
        }
    }
}
