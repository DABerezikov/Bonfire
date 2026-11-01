using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planting;

public sealed class DbGreenhousePropertiesWriter(IUnitOfWorkFactory units) : IGreenhousePropertiesWriter
{
    public async Task<Result<GreenhousePropertiesResultFromViewModel>> ApplyAsync(GreenhousePropertiesEdit edit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(edit);
        cancellationToken.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        var house = await unit.Repository<Greenhouse>().GetAsync(edit.Id, cancellationToken).ConfigureAwait(false);
        if (house is null) return Result.Failure<GreenhousePropertiesResultFromViewModel>("Теплица уже удалена");
        var updated = GreenhousePropertiesDbMapper.Read(house).Bind(current => GreenhousePropertiesDbMapper
            .Contents(house)
            .Bind(contents => edit.ApplyTo(current, contents)));
        return await updated.Match(async state =>
            {
                GreenhousePropertiesDbMapper.Apply(state, house);
                try
                {
                    await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    return Result.Success(GreenhousePropertiesDbMapper.Receipt(state));
                }
                catch (DbUpdateConcurrencyException)
                {
                    return Result.Failure<GreenhousePropertiesResultFromViewModel>("Запись изменена другим процессом");
                }
                catch (DbUpdateException error)
                {
                    return Result.Failure<GreenhousePropertiesResultFromViewModel>(
                        $"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
                }
            }, error => Task.FromResult(Result.Failure<GreenhousePropertiesResultFromViewModel>(error)))
            .ConfigureAwait(false);
    }
}