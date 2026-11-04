using System.Collections.Immutable;
using System.Data.Common;
using Bonfire.Application.Libraries;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.SeedEditing;

public sealed class DbSeedEditorOptionsReader(IUnitOfWorkFactory units) : ISeedEditorOptionsReader
{
    public async Task<Result<SeedEditorOptionsFromViewModel>> ReadAsync(CancellationToken cancellationToken)
    {
        await using var unit = units.Create();
        try
        {
            var sorts = await unit.Repository<PlantSort>().Rows.Select(sort => new
                { sort.Id, sort.Name, ProducerId = EF.Property<int>(sort, "ProducerId"), sort.PlantColor, sort.Maturity })
                .ToArrayAsync(cancellationToken).ConfigureAwait(false);
            var links = await unit.Repository<Plant>().Rows.Select(plant => new
                { SortId = EF.Property<int>(plant, "PlantSortId"), CultureId = EF.Property<int>(plant, "PlantCultureId") })
                .ToArrayAsync(cancellationToken).ConfigureAwait(false);
            var cultures = await unit.Repository<PlantCulture>().Rows.OrderBy(culture => culture.Name)
                .Select(culture => new CultureDetailsFromViewModel(culture.Id, culture.Name, culture.Class))
                .ToArrayAsync(cancellationToken).ConfigureAwait(false);
            var options = await unit.Repository<SortOption>().Rows
                .Select(option => new { option.Category, option.Name })
                .ToArrayAsync(cancellationToken).ConfigureAwait(false);
            var producers = await unit.Repository<Producer>().Rows.OrderBy(producer => producer.Name)
                .Select(producer => new ProducerDetailsFromViewModel(producer.Id, producer.Name))
                .ToArrayAsync(cancellationToken).ConfigureAwait(false);

            static ImmutableArray<string> Values(IEnumerable<string?> values) => values
                .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToImmutableArray();

            return Result.Success(new SeedEditorOptionsFromViewModel(
                Values(sorts.Select(sort => sort.PlantColor)
                    .Concat(options.Where(option => option.Category == "Цвет").Select(option => option.Name))),
                Values(sorts.Select(sort => sort.Maturity)
                    .Concat(options.Where(option => option.Category == "Созревание").Select(option => option.Name))),
                producers.ToImmutableArray())
            {
                Catalog = Option.Some(new SeedCatalogFromViewModel(cultures.ToImmutableArray(),
                    sorts.Select(sort => new CatalogSortFromViewModel(sort.Id, sort.Name, sort.ProducerId,
                        links.Where(link => link.SortId == sort.Id).Select(link => link.CultureId).Distinct().ToImmutableArray()))
                        .ToImmutableArray(), producers.ToImmutableArray()))
            });
        }
        catch (DbException error)
        {
            return Result.Failure<SeedEditorOptionsFromViewModel>(
                $"Не удалось прочитать варианты редактора: {error.Message}");
        }
    }
}
