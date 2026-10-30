using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planting;

public sealed class DbGardenDeletionWriter(IUnitOfWorkFactory units) : IGardenDeletionWriter
{
    public async Task<Result<GardenDeletionResultFromViewModel>> DeleteAsync(GardenId id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);
        cancellationToken.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        var repository = unit.Repository<Garden>();
        var current = await repository.Items.Where(h => h.Id == id.Value)
            .Select(h => new { HasGreenhouses = h.Greenhouses.Any() })
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (current is null) return Result.Failure<GardenDeletionResultFromViewModel>("Участок уже удалён");
        if (current.HasGreenhouses)
            return Result.Failure<GardenDeletionResultFromViewModel>("Нельзя удалить участок с теплицами");
        try
        {
            await repository.RemoveAsync(id.Value, cancellationToken).ConfigureAwait(false);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(new GardenDeletionResultFromViewModel(id.Value));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<GardenDeletionResultFromViewModel>("Запись изменена другим процессом");
        }
        catch (DbUpdateException error)
        {
            return Result.Failure<GardenDeletionResultFromViewModel>($"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
        }
    }
}
