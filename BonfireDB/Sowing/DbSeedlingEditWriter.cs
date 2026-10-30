using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;
using BonfireDB.Entities.Base;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Sowing;

public sealed class DbSeedlingEditWriter(IUnitOfWorkFactory units) : ISeedlingEditWriter
{
    public async Task<Result<SeedlingEditResultFromViewModel>> SaveAsync(SeedlingEdit edit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(edit);
        cancellationToken.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        var batch = await unit.Repository<Seedling>().GetAsync(edit.Id.Value, cancellationToken).ConfigureAwait(false);
        if (batch is null) return Result.Failure<SeedlingEditResultFromViewModel>("Партия рассады уже удалена");
        if (batch.SeedlingInfos.Count == 0) return Result.Failure<SeedlingEditResultFromViewModel>("В партии нет записи метаданных");
        return await SeedlingBatchDbMapper.Read(batch).Bind(edit.ApplyTo).Match(async changed =>
        {
            SeedlingBatchDbMapper.Apply(changed, batch);
            SeedlingDetailsDbMapper.Apply(edit.Details, batch);
            try
            {
                await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return Result.Success(new SeedlingEditResultFromViewModel(edit.Details, SeedlingBatchDbMapper.Receipt(batch)));
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<SeedlingEditResultFromViewModel>("Запись изменена другим процессом");
            }
            catch (DbUpdateException error)
            {
                return Result.Failure<SeedlingEditResultFromViewModel>($"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
            }
        }, error => Task.FromResult(Result.Failure<SeedlingEditResultFromViewModel>(error))).ConfigureAwait(false);
    }
}
