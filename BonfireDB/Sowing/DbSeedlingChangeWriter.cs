using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Sowing;

public sealed class DbSeedlingChangeWriter(IUnitOfWorkFactory units) : ISeedlingChangeWriter
{
    public async Task<Result<SeedlingChangeResultFromViewModel>> ApplyAsync(SeedlingId id, SeedlingAction action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        var batch = await unit.Repository<Seedling>().GetAsync(id.Value, cancellationToken).ConfigureAwait(false);
        if (batch is null) return Result.Failure<SeedlingChangeResultFromViewModel>("Партия рассады уже удалена");
        return await SeedlingBatchDbMapper.Read(batch).Bind(model => model.Apply(action)).Match(async changed =>
        {
            SeedlingBatchDbMapper.Apply(changed, batch);
            try
            {
                await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return Result.Success(SeedlingBatchDbMapper.Receipt(batch));
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<SeedlingChangeResultFromViewModel>("Запись изменена другим процессом");
            }
            catch (DbUpdateException error)
            {
                return Result.Failure<SeedlingChangeResultFromViewModel>(
                    $"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
            }
        }, error => Task.FromResult(Result.Failure<SeedlingChangeResultFromViewModel>(error))).ConfigureAwait(false);
    }
}