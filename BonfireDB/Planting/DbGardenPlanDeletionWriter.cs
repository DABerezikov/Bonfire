using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planting;

public sealed class DbGardenPlanDeletionWriter(IUnitOfWorkFactory units) : IGardenPlanDeletionWriter
{
    public async Task<Result<GardenPlanDeletionResultFromViewModel>> DeleteAsync(GardenPlanId id,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);
        cancellationToken.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        var repository = unit.Repository<GardenPlan>();
        var current = await repository.Items.Where(h => h.Id == id.Value)
            .Select(h => new { HasGreenhouses = h.Gardens.Any(g => g.Greenhouses.Any()) })
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (current is null) return Result.Failure<GardenPlanDeletionResultFromViewModel>("План уже удалён");
        if (current.HasGreenhouses)
            return Result.Failure<GardenPlanDeletionResultFromViewModel>("Нельзя удалить план с теплицами");
        try
        {
            await repository.RemoveAsync(id.Value, cancellationToken).ConfigureAwait(false);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(new GardenPlanDeletionResultFromViewModel(id.Value));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<GardenPlanDeletionResultFromViewModel>("Запись изменена другим процессом");
        }
        catch (DbUpdateException error)
        {
            return Result.Failure<GardenPlanDeletionResultFromViewModel>(
                $"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
        }
    }
}