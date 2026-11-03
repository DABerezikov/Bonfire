using System.Collections.Immutable;
using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Libraries;

public sealed class DbSortOptionStore(IUnitOfWorkFactory units) : ISortOptionStore
{
    private static string Category(SortOptionCategory category) => category == SortOptionCategory.Color ? "Цвет" : "Созревание";
    private static string? Text(PlantSort sort, SortOptionCategory category) => category == SortOptionCategory.Color ? sort.PlantColor : sort.Maturity;
    private static bool Same(string? left, string right) => string.Equals(LibraryName.NormalizeWhitespace(left ?? string.Empty), right, StringComparison.OrdinalIgnoreCase);

    public Task<Result<ImmutableArray<SortOptionFromViewModel>>> ReadAsync(CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var sorts = await unit.Repository<PlantSort>().Rows.AsNoTracking()
                .Select(sort => new { sort.PlantColor, sort.Maturity }).ToArrayAsync(cancellationToken);
            var options = await unit.Repository<SortOption>().Rows.AsNoTracking()
                .Select(option => new { option.Category, option.Name }).ToArrayAsync(cancellationToken);
            var result = ImmutableArray.CreateBuilder<SortOptionFromViewModel>();
            foreach (var category in new[] { SortOptionCategory.Color, SortOptionCategory.Maturity })
            {
                var used = sorts.Select(sort => category == SortOptionCategory.Color ? sort.PlantColor : sort.Maturity)
                    .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => LibraryName.NormalizeWhitespace(value!))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var names = used.Concat(options.Where(option => option.Category == Category(category)).Select(option => LibraryName.NormalizeWhitespace(option.Name)))
                    .Where(name => name.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name);
                result.AddRange(names.Select(name => new SortOptionFromViewModel(category, name, used.Contains(name))));
            }
            return Result.Success(result.ToImmutable());
        }, "справочников цвета и созревания");

    public Task<Result<SortOptionFromViewModel>> SaveAsync(Option<SortOptionName> original, SortOptionName value, CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var options = await unit.Repository<SortOption>().Rows.Where(option => option.Category == Category(value.Category))
                .ToArrayAsync(cancellationToken);
            var sorts = await unit.Repository<PlantSort>().Rows.ToArrayAsync(cancellationToken);
            var old = original.Match(name => name.Name.Value, () => value.Name.Value);
            var existing = options.Where(option => Same(option.Name, old)).ToArray();
            var used = sorts.Where(sort => Same(Text(sort, value.Category), old)).ToArray();
            if (original is Option<SortOptionName>.None && (existing.Length > 0 || used.Length > 0))
                return Result.Failure<SortOptionFromViewModel>("Такое значение уже существует.");
            if (original is Option<SortOptionName>.Some && existing.Length == 0 && used.Length == 0)
                return Result.Failure<SortOptionFromViewModel>("Значение уже удалено или переименовано. Обновите справочник.");
            if (!Same(old, value.Name.Value) && (options.Any(option => Same(option.Name, value.Name.Value))
                || sorts.Any(sort => Same(Text(sort, value.Category), value.Name.Value))))
                return Result.Failure<SortOptionFromViewModel>("Такое значение уже существует. Выберите другое название.");
            if (existing.Length == 0) unit.Repository<SortOption>().Add(new() { Category = Category(value.Category), Name = value.Name.Value });
            else foreach (var option in existing) option.Name = value.Name.Value;
            foreach (var sort in used)
                if (value.Category == SortOptionCategory.Color) sort.PlantColor = value.Name.Value;
                else sort.Maturity = value.Name.Value;
            return await Save(unit, new SortOptionFromViewModel(value.Category, value.Name.Value, used.Length > 0), cancellationToken);
        }, "справочников цвета и созревания");

    public Task<Result<bool>> DeleteAsync(SortOptionName value, CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var sorts = await unit.Repository<PlantSort>().Rows.AsNoTracking()
                .Select(sort => new { sort.PlantColor, sort.Maturity }).ToArrayAsync(cancellationToken);
            if (sorts.Any(sort => Same(value.Category == SortOptionCategory.Color ? sort.PlantColor : sort.Maturity, value.Name.Value)))
                return Result.Failure<bool>("Значение используется в характеристиках сортов. Сначала замените его в связанных записях.");
            var options = await unit.Repository<SortOption>().Rows.Where(option => option.Category == Category(value.Category)).ToArrayAsync(cancellationToken);
            var found = options.Where(option => Same(option.Name, value.Name.Value)).ToArray();
            if (found.Length == 0) return Result.Failure<bool>("Значение не найдено. Обновите справочник.");
            foreach (var option in found) await unit.Repository<SortOption>().RemoveAsync(option.Id, cancellationToken);
            return await Save(unit, true, cancellationToken);
        }, "справочников цвета и созревания");

    private static async Task<Result<T>> Save<T>(IUnitOfWork unit, T result, CancellationToken cancellationToken)
    {
        try { await unit.SaveChangesAsync(cancellationToken); return Result.Success(result); }
        catch (DbUpdateException error) { return Result.Failure<T>($"Не удалось сохранить справочник: {error.InnerException?.Message ?? error.Message}"); }
    }
}
