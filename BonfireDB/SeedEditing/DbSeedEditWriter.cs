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

        ApplyPackage(edit.Package.Values, saved.SeedsInfo);
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

    private static void ApplyPackage(SeedPackageValues values, SeedsInfo target)
    {
        target.WeightPack = values.WeightPack;
        target.QuantityPack = values.QuantityPack;
        target.AmountSeeds = values.AmountSeeds;
        target.AmountSeedsWeight = ToNullable(values.AmountSeedsWeight);
        target.CostPack = values.CostPack;
        target.PurchaseDate = values.PurchaseDate;
        target.ExpirationDate = values.ExpirationDate;
        target.Note = ToNullable(values.Note);
        target.SeedSource = ToNullable(values.SeedSource);
        target.DisposeComment = ToNullable(values.DisposeComment);
    }

    private static void ApplyCharacteristics(SortCharacteristics details, PlantSort target)
    {
        target.Name = details.Name.Value;
        var values = details.Values;
        target.Description = ToNullable(values.Description);
        target.MinGerminationTime = ToNullable(values.MinGerminationTime);
        target.MaxGerminationTime = ToNullable(values.MaxGerminationTime);
        target.AgeOfSeedlings = ToNullable(values.AgeOfSeedlings);
        target.MaxAgeOfSeedlings = ToNullable(values.MaxAgeOfSeedlings);
        target.GrowingSeason = ToNullable(values.GrowingSeason);
        target.MaxGrowingSeason = ToNullable(values.MaxGrowingSeason);
        target.LandingPattern = ToNullable(values.LandingPattern);
        target.LandingRowSpacing = ToNullable(values.LandingRowSpacing);
        target.PlantHeight = ToNullable(values.PlantHeight);
        target.MaxPlantHeight = ToNullable(values.MaxPlantHeight);
        target.PlantColor = ToNullable(values.PlantColor);
        target.Maturity = ToNullable(values.Maturity);
        target.YieldKg = ToNullable(values.YieldKg);
        target.MinFruitWeight = ToNullable(values.MinFruitWeight);
        target.MaxFruitWeight = ToNullable(values.MaxFruitWeight);
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

    private static T? ToNullable<T>(Option<T> value) where T : struct => value.Match<T?>(present => present, () => null);
    private static string? ToNullable(Option<string> value) => value.Match<string?>(present => present, () => null);
}
