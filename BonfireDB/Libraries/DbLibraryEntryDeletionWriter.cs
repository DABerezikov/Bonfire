using System.Diagnostics;
using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.Planning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Libraries;

public sealed class DbLibraryEntryDeletionWriter(IUnitOfWorkFactory units) : ILibraryEntryDeletionWriter
{
    public Task<Result<bool>> DeleteAsync(LibraryEntry entry, CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var used = entry switch
            {
                LibraryEntry.Culture culture => await unit.Repository<Plant>().Rows.AnyAsync(p => p.PlantCulture.Id == culture.Id.Value, cancellationToken)
                    || await unit.Repository<CultureGrowingProfileRow>().Rows.AnyAsync(p => p.CultureId == culture.Id.Value, cancellationToken)
                    || await unit.Repository<RegionalPlantingWindowRow>().Rows.AnyAsync(p => p.CultureId == culture.Id.Value, cancellationToken)
                    || await unit.Repository<PlannedBatchRow>().Rows.AnyAsync(p => p.CultureId == culture.Id.Value, cancellationToken)
                    || await unit.Repository<PerennialPlantingRow>().Rows.AnyAsync(p => p.CultureId == culture.Id.Value, cancellationToken)
                    || await unit.Repository<HarvestEntryRow>().Rows.AnyAsync(p => p.CultureId == culture.Id.Value, cancellationToken),
                LibraryEntry.Sort sort => await unit.Repository<Plant>().Rows.AnyAsync(p => p.PlantSort.Id == sort.Id.Value, cancellationToken)
                    || await unit.Repository<SortPlanningSettingsRow>().Rows.AnyAsync(p => p.SortId == sort.Id.Value, cancellationToken)
                    || await unit.Repository<PlannedBatchRow>().Rows.AnyAsync(p => p.SortId == sort.Id.Value, cancellationToken)
                    || await unit.Repository<PerennialPlantingRow>().Rows.AnyAsync(p => p.SortId == sort.Id.Value, cancellationToken)
                    || await unit.Repository<HarvestEntryRow>().Rows.AnyAsync(p => p.SortId == sort.Id.Value, cancellationToken),
                LibraryEntry.Producer producer => await unit.Repository<PlantSort>().Rows.AnyAsync(p => p.Producer.Id == producer.Id.Value, cancellationToken),
                _ => throw new UnreachableException()
            };
            if (used) return Result.Failure<bool>("Значение используется в связанных записях. Сначала удалите или измените эти связи.");
            var removed = entry switch
            {
                LibraryEntry.Culture culture => await Remove<PlantCulture>(unit, culture.Id.Value, cancellationToken),
                LibraryEntry.Sort sort => await Remove<PlantSort>(unit, sort.Id.Value, cancellationToken),
                LibraryEntry.Producer producer => await Remove<Producer>(unit, producer.Id.Value, cancellationToken),
                _ => throw new UnreachableException()
            };
            if (!removed) return Result.Failure<bool>("Запись не найдена. Обновите библиотеку.");
            try { await unit.SaveChangesAsync(cancellationToken); return Result.Success(true); }
            catch (DbUpdateException error) { return Result.Failure<bool>($"Не удалось удалить запись: {error.InnerException?.Message ?? error.Message}"); }
        }, "библиотеки");

    private static async Task<bool> Remove<T>(IUnitOfWork unit, int id, CancellationToken cancellationToken) where T : class, IEntity
    {
        if (!await unit.Repository<T>().Rows.AnyAsync(row => row.Id == id, cancellationToken)) return false;
        await unit.Repository<T>().RemoveAsync(id, cancellationToken); return true;
    }
}
