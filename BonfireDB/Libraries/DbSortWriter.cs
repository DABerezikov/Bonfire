using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Libraries;

public sealed class DbSortWriter(IUnitOfWorkFactory units) : ISortWriter
{
    public async Task<Result<SortDetailsFromViewModel>> SaveAsync(SortLibraryEdit edit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(edit);
        await using var unit = units.Create();
        var repository = unit.Repository<PlantSort>();
        // PlantSortRepository загружает все Plants вместе с PlantCulture.
        var entity = await repository.GetAsync(edit.Id.Value, cancellationToken).ConfigureAwait(false);
        if (entity is null) return Result.Failure<SortDetailsFromViewModel>("Сорт не найден (удалён?)");

        var selectedCulture = await edit.CultureChange.Match(async identity =>
        {
            var culture = await unit.Repository<PlantCulture>().GetAsync(identity.Value, cancellationToken)
                .ConfigureAwait(false);
            return culture is null
                ? Result.Failure<Option<PlantCulture>>("Культура не найдена")
                : Result.Success(Option.Some(culture));
        }, () => Task.FromResult(Result.Success(Option.None<PlantCulture>()))).ConfigureAwait(false);

        return await selectedCulture.Match(async culture =>
        {
            entity.Name = edit.Name.Value;
            if (culture is Option<PlantCulture>.Some selected)
                foreach (var plant in entity.Plants)
                    plant.PlantCulture = selected.Value;
            try
            {
                await repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
                await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return Result.Success(new SortDetailsFromViewModel(entity.Id, entity.Name,
                    Array.AsReadOnly(entity.Plants.Select(plant => new SortPlantCultureFromViewModel(plant.Id,
                        new CultureDetailsFromViewModel(plant.PlantCulture.Id, plant.PlantCulture.Name,
                            plant.PlantCulture.Class))).ToArray())));
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<SortDetailsFromViewModel>("Запись изменена другим процессом");
            }
            catch (DbUpdateException error)
            {
                return Result.Failure<SortDetailsFromViewModel>(
                    $"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
            }
        }, error => Task.FromResult(Result.Failure<SortDetailsFromViewModel>(error))).ConfigureAwait(false);
    }
}