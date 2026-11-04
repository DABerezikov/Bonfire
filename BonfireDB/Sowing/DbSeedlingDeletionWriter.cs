using System.Diagnostics;
using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;
using Bonfire.Domain.Seeds;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using BonfireDB.SeedEditing;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Sowing;

public sealed class DbSeedlingDeletionWriter(IUnitOfWorkFactory units) : ISeedlingDeletionWriter
{
    public async Task<Result<SeedlingDeletionResultFromViewModel>> DeleteAsync(SeedlingId id, SeedlingRemoval removal,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(removal);
        cancellationToken.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        var batches = unit.Repository<Seedling>();
        var batch = await batches.GetAsync(id.Value, cancellationToken).ConfigureAwait(false);
        if (batch is null) return Result.Failure<SeedlingDeletionResultFromViewModel>("Партия рассады уже удалена");

        var prepared = removal switch
        {
            SeedlingRemoval.Discard => Result.Success(Option.None<ReturnedSeedStockFromViewModel>()),
            SeedlingRemoval.CorrectSowing correction => await ReturnAsync(unit, batch, correction, cancellationToken)
                .ConfigureAwait(false),
            _ => throw new UnreachableException()
        };

        return await prepared.Match(async returned =>
        {
            try
            {
                await batches.RemoveAsync(batch.Id, cancellationToken).ConfigureAwait(false);
                await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return Result.Success(new SeedlingDeletionResultFromViewModel(batch.Id, returned));
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<SeedlingDeletionResultFromViewModel>("Запись изменена другим процессом");
            }
            catch (DbUpdateException error)
            {
                return Result.Failure<SeedlingDeletionResultFromViewModel>(
                    $"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
            }
        }, error => Task.FromResult(Result.Failure<SeedlingDeletionResultFromViewModel>(error))).ConfigureAwait(false);
    }

    private static async Task<Result<Option<ReturnedSeedStockFromViewModel>>> ReturnAsync(
        IUnitOfWork unit, Seedling batch, SeedlingRemoval.CorrectSowing correction, CancellationToken cancellationToken)
    {
        var seed = await unit.Repository<Seed>().GetAsync(batch.SeedId, cancellationToken).ConfigureAwait(false);
        if (seed is null)
            return Result.Failure<Option<ReturnedSeedStockFromViewModel>>(
                "Исходный пакет семян удалён. Можно удалить рассаду без возврата семян.");

        return SeedPackage.Create(SeedPackageDbMapper.Read(seed.SeedsInfo))
            .Bind(package => correction.ReturnTo(package, batch.Quantity, batch.Weight))
            .Bind(package =>
            {
                SeedPackageDbMapper.Apply(package.Values, seed.SeedsInfo);
                return Result.Success(Option.Some(new ReturnedSeedStockFromViewModel(seed.Id,
                    package.Values.AmountSeeds, package.Values.AmountSeedsWeight)));
            });
    }
}