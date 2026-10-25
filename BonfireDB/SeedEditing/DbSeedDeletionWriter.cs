using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.SeedEditing;

public sealed class DbSeedDeletionWriter(IUnitOfWorkFactory units) : ISeedDeletionWriter
{
    public async Task<Result<SeedDeletionResultFromViewModel>> DeleteAsync(SeedDeletion deletion, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deletion);
        await using var unit = units.Create();
        var seeds = unit.Repository<Seed>();
        var seed = await seeds.GetAsync(deletion.SeedId.Value, cancellationToken).ConfigureAwait(false);
        if (seed is null) return Result.Failure<SeedDeletionResultFromViewModel>("Запись семян удалена");
        // SeedsInfo — principal связи; причина остаётся после удаления dependent Seed.
        seed.SeedsInfo.DisposeComment = deletion.Reason;
        try
        {
            await seeds.RemoveAsync(seed.Id, cancellationToken).ConfigureAwait(false);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(new SeedDeletionResultFromViewModel(seed.Id));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<SeedDeletionResultFromViewModel>("Запись изменена другим процессом");
        }
        catch (DbUpdateException error)
        {
            return Result.Failure<SeedDeletionResultFromViewModel>($"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
        }
    }
}
