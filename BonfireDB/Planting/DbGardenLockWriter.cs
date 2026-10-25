using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planting;

public sealed class DbGardenLockWriter(IUnitOfWorkFactory units) : IGardenLockWriter
{
    public async Task<Result<GardenLockResultFromViewModel>> ApplyAsync(GardenLockChange change, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(change);
        cancellationToken.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        switch (change.Target)
        {
            case GardenLockTarget.Element:
                var element = await unit.Repository<GardenElement>().GetAsync(change.Id, cancellationToken).ConfigureAwait(false);
                if (element is null) return Result.Failure<GardenLockResultFromViewModel>("Элемент уже удалён");
                element.IsLocked = change.IsLocked;
                break;
            case GardenLockTarget.Greenhouse:
                var house = await unit.Repository<Greenhouse>().GetAsync(change.Id, cancellationToken).ConfigureAwait(false);
                if (house is null) return Result.Failure<GardenLockResultFromViewModel>("Теплица уже удалена");
                house.IsLocked = change.IsLocked;
                break;
            default:
                throw new System.Diagnostics.UnreachableException();
        }
        try
        {
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(new GardenLockResultFromViewModel(change.Id, change.IsLocked));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<GardenLockResultFromViewModel>("Запись изменена другим процессом");
        }
        catch (DbUpdateException error)
        {
            return Result.Failure<GardenLockResultFromViewModel>($"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
        }
    }
}
