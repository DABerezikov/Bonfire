using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Libraries;

public sealed class DbProducerWriter(IUnitOfWorkFactory units) : IProducerWriter
{
    public async Task<Result<ProducerDetailsFromViewModel>> SaveAsync(ProducerDefinition producer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(producer);
        await using var unit = units.Create();
        var repository = unit.Repository<Producer>();
        var entity = await repository.GetAsync(producer.Id.Value, cancellationToken).ConfigureAwait(false);
        if (entity is null) return Result.Failure<ProducerDetailsFromViewModel>("Производитель не найден (удалён?)");

        entity.Name = producer.Name.Value;

        try
        {
            await repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(new ProducerDetailsFromViewModel(entity.Id, entity.Name));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<ProducerDetailsFromViewModel>("Запись изменена другим процессом");
        }
        catch (DbUpdateException error)
        {
            return Result.Failure<ProducerDetailsFromViewModel>(
                $"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
        }
    }
}