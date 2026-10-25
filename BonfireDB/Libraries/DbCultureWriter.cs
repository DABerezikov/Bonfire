using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Libraries;

public sealed class DbCultureWriter(IUnitOfWorkFactory units) : ICultureWriter
{
    public async Task<Result<CultureDetailsFromViewModel>> SaveAsync(CultureDefinition culture, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(culture);
        await using var unit = units.Create();
        var repository = unit.Repository<PlantCulture>();
        var entity = await repository.GetAsync(culture.Id.Value, cancellationToken).ConfigureAwait(false);
        if (entity is null) return Result.Failure<CultureDetailsFromViewModel>("Культура не найдена (удалёна?)");

        entity.Name = culture.Name;
        entity.Class = culture.Class;
        try
        {
            await repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(new CultureDetailsFromViewModel(entity.Id, entity.Name, entity.Class));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<CultureDetailsFromViewModel>("Запись изменена другим процессом");
        }
        catch (DbUpdateException error)
        {
            return Result.Failure<CultureDetailsFromViewModel>($"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
        }
    }
}
