using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planting;

public sealed class DbGreenhouseDeletionWriter(IUnitOfWorkFactory units) : IGreenhouseDeletionWriter
{
    public async Task<Result<GreenhouseDeletionResultFromViewModel>> DeleteAsync(GreenhouseId id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);
        cancellationToken.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        var repository = unit.Repository<Greenhouse>();
        var current = await repository.Items.Where(h => h.Id == id.Value)
            .Select(h => new { HasNestedGreenhouses = h.Greenhouses.Any() })
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (current is null) return Result.Failure<GreenhouseDeletionResultFromViewModel>("Теплица уже удалена");
        if (current.HasNestedGreenhouses)
            return Result.Failure<GreenhouseDeletionResultFromViewModel>("Нельзя удалить теплицу с вложенными теплицами");
        try
        {
            await repository.RemoveAsync(id.Value, cancellationToken).ConfigureAwait(false);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(new GreenhouseDeletionResultFromViewModel(id.Value));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<GreenhouseDeletionResultFromViewModel>("Запись изменена другим процессом");
        }
        catch (DbUpdateException error)
        {
            return Result.Failure<GreenhouseDeletionResultFromViewModel>($"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
        }
    }
}
