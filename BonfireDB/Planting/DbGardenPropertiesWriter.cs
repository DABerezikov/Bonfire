using Bonfire.Application.Garden;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planting;

public sealed class DbGardenPropertiesWriter(IUnitOfWorkFactory units) : IGardenPropertiesWriter
{
    public async Task<Result<GardenPropertiesResultFromViewModel>> ApplyAsync(GardenPropertiesEdit edit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(edit);
        cancellationToken.ThrowIfCancellationRequested();
        await using var unit = units.Create();
        var garden = await unit.Repository<Garden>().GetAsync(edit.Id, cancellationToken).ConfigureAwait(false);
        if (garden is null) return Result.Failure<GardenPropertiesResultFromViewModel>("Участок уже удалён");
        var updated = GardenPropertiesDbMapper.Read(garden).Bind(current => GardenPropertiesDbMapper.Contents(garden)
            .Bind(contents => edit.ApplyTo(current, contents)));
        return await updated.Match(async state =>
        {
            GardenPropertiesDbMapper.Apply(state, garden);
            try
            {
                await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return Result.Success(GardenPropertiesDbMapper.Receipt(state));
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<GardenPropertiesResultFromViewModel>("Запись изменена другим процессом");
            }
            catch (DbUpdateException error)
            {
                return Result.Failure<GardenPropertiesResultFromViewModel>(
                    $"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
            }
        }, error => Task.FromResult(Result.Failure<GardenPropertiesResultFromViewModel>(error))).ConfigureAwait(false);
    }
}