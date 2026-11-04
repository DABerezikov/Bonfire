using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;
using Bonfire.Domain.Libraries;
using BonfireDB.Libraries;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.SeedEditing;

public sealed class DbSeedAcquisitionWriter(IUnitOfWorkFactory units) : ISeedAcquisitionWriter
{
    public async Task<Result<AcquisitionResultFromViewModel>> SaveAsync(SeedAcquisition acquisition,
        DateTime purchasedAt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(acquisition);
        await using var unit = units.Create();
        var seeds = unit.Repository<Seed>();
        var inventory = await seeds.Items.OrderBy(seed => seed.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var values = acquisition.Values;
        var plant = await ResolvePlantAsync(values, inventory, unit, cancellationToken).ConfigureAwait(false);
        // Назначение производителя общему сорту предшествует поиску пакета, как в прежнем сценарии.
        var existing = inventory.FirstOrDefault(seed => seed.SeedsInfo.ExpirationDate.Year == values.BestBy.Year
                                                        && SameName(seed.Plant.PlantSort.Producer.Name, values.Producer) &&
                                                        SameName(seed.Plant.PlantCulture.Name, values.Culture)
                                                        && SameName(seed.Plant.PlantSort.Name, values.Sort));
        var package = existing is { } stored
            ? SeedPackage.Create(SeedPackageDbMapper.Read(stored.SeedsInfo))
                .Bind(current => acquisition.Replenish(current, purchasedAt))
            : acquisition.CreatePackage(purchasedAt);

        return await package.Match(async calculated =>
        {
            try
            {
                Seed target;
                if (existing is { } current)
                {
                    target = current;
                }
                else
                {
                    target = new Seed { Plant = plant, SeedsInfo = new SeedsInfo() };
                    target.SeedsInfo.Seed = target;
                    await seeds.AddAsync(target, cancellationToken).ConfigureAwait(false);
                }

                target.Plant.PlantCulture.Name = values.Culture;
                target.Plant.PlantSort.Name = values.Sort;
                target.Plant.PlantSort.Producer.Name = values.Producer;
                SeedPackageDbMapper.Apply(calculated.Values, target.SeedsInfo);
                await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                AcquisitionResultFromViewModel result = existing is null
                    ? new AcquisitionResultFromViewModel.Created(target.Id)
                    : new AcquisitionResultFromViewModel.Replenished(target.Id);
                return Result.Success(result);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<AcquisitionResultFromViewModel>("Запись изменена другим процессом");
            }
            catch (DbUpdateException error)
            {
                return Result.Failure<AcquisitionResultFromViewModel>(
                    $"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
            }
        }, error => Task.FromResult(Result.Failure<AcquisitionResultFromViewModel>(error))).ConfigureAwait(false);
    }

    private static bool SameName(string stored, string name) => LibraryName.NormalizeWhitespace(stored) == name;

    private static async Task<Plant> ResolvePlantAsync(SeedAcquisitionValues values, IReadOnlyList<Seed> inventory,
        IUnitOfWork unit, CancellationToken cancellationToken)
    {
        var matching = inventory.FirstOrDefault(seed => SameName(seed.Plant.PlantCulture.Name, values.Culture)
            && SameName(seed.Plant.PlantSort.Name, values.Sort) && SameName(seed.Plant.PlantSort.Producer.Name, values.Producer));
        if (matching is not null) return matching.Plant;
        var culture = (await DbLibraryNameLookup.ReadAsync(unit.Repository<PlantCulture>().Rows, values.Culture, cancellationToken))
            .Match(row => row, () => new PlantCulture { Name = values.Culture, Class = values.Class });
        var producer = (await DbLibraryNameLookup.ReadAsync(unit.Repository<Producer>().Rows, values.Producer, cancellationToken))
            .Match(row => row, () => new Producer { Name = values.Producer });
        var sorts = unit.Repository<PlantSort>().Rows.Include(sort => sort.Producer);
        var sameProducer = await DbLibraryNameLookup.ReadAsync(sorts.Where(sort => sort.Producer.Id == producer.Id), values.Sort, cancellationToken);
        var selected = sameProducer is Option<PlantSort>.Some ? sameProducer
            : await DbLibraryNameLookup.ReadAsync(sorts, values.Sort, cancellationToken);
        var sort = selected.Match(row => row, () => new PlantSort { Name = values.Sort, Producer = producer });
        sort.Producer = producer;
        var existing = await unit.Repository<Plant>().Rows
            .FirstOrDefaultAsync(plant => plant.PlantCulture.Id == culture.Id && plant.PlantSort.Id == sort.Id, cancellationToken)
            .ConfigureAwait(false);
        return existing ?? new Plant { PlantCulture = culture, PlantSort = sort };
    }
}
