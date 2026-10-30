using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.SeedEditing;

public sealed class DbSeedEditWriter(IUnitOfWorkFactory units) : ISeedEditWriter
{
    public async Task<Result<SeedEditResultFromViewModel>> SaveAsync(SeedEdit edit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(edit);
        await using var unit = units.Create();
        var saved = await unit.Repository<Seed>().GetAsync(edit.SeedId.Value, cancellationToken).ConfigureAwait(false);
        if (saved is null) return Result.Failure<SeedEditResultFromViewModel>("Запись семян удалена");
        if (saved.Plant.PlantSort.Id != edit.SortId.Value)
            return Result.Failure<SeedEditResultFromViewModel>("Сорт записи изменён; откройте её повторно");
        var producer = await unit.Repository<Producer>().GetAsync(edit.ProducerId.Value, cancellationToken).ConfigureAwait(false);
        if (producer is null) return Result.Failure<SeedEditResultFromViewModel>("Производитель не найден");

        SeedPackageDbMapper.Apply(edit.Package.Values, saved.SeedsInfo);
        ApplyCharacteristics(edit.Characteristics, saved.Plant.PlantSort);
        saved.Plant.PlantSort.Producer = producer;
        try
        {
            await RememberOptionAsync(unit, "Цвет", edit.Characteristics.Values.PlantColor, cancellationToken).ConfigureAwait(false);
            await RememberOptionAsync(unit, "Созревание", edit.Characteristics.Values.Maturity, cancellationToken).ConfigureAwait(false);
            // Свежий граф отслеживается: пакет, общий сорт и варианты сохраняются одной транзакцией EF.
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(new SeedEditResultFromViewModel(saved.Id, saved.Plant.PlantSort.Id));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<SeedEditResultFromViewModel>("Запись изменена другим процессом");
        }
        catch (DbUpdateException error)
        {
            return Result.Failure<SeedEditResultFromViewModel>($"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
        }
    }

    private static void ApplyCharacteristics(SortCharacteristics details, PlantSort target)
    {
        target.Name = details.Name.Value;
        var values = details.Values;
        SortCharacteristicsDbMapper.Apply(values, target);
    }

    private static Task RememberOptionAsync(IUnitOfWork unit, string category, Option<string> value, CancellationToken cancellationToken)
        => value.Match(async text =>
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var name = text.Trim();
            var options = unit.Repository<SortOption>();
            if (!await options.Items.AnyAsync(option => option.Category == category && option.Name == name, cancellationToken).ConfigureAwait(false))
                await options.AddAsync(new SortOption { Category = category, Name = name }, cancellationToken).ConfigureAwait(false);
        }, () => Task.CompletedTask);
}
