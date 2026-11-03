using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;
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
        var plant = await ResolvePlantAsync(values, unit, cancellationToken).ConfigureAwait(false);
        // Назначение производителя общему сорту предшествует поиску пакета, как в прежнем сценарии.
        var existing = inventory.FirstOrDefault(seed => seed.SeedsInfo.ExpirationDate.Year == values.BestBy.Year
                                                        && seed.Plant.PlantSort.Producer.Name == values.Producer &&
                                                        seed.Plant.PlantCulture.Name == values.Culture
                                                        && seed.Plant.PlantSort.Name == values.Sort);
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

    private static async Task<Plant> ResolvePlantAsync(SeedAcquisitionValues values, IUnitOfWork unit, CancellationToken cancellationToken)
    {
        var exact = await unit.Repository<Plant>().Rows.Include(plant => plant.PlantCulture)
            .Include(plant => plant.PlantSort).ThenInclude(sort => sort.Producer)
            .OrderBy(plant => plant.Id).FirstOrDefaultAsync(plant => plant.PlantCulture.Name == values.Culture
                && plant.PlantSort.Name == values.Sort && plant.PlantSort.Producer.Name == values.Producer, cancellationToken)
            .ConfigureAwait(false);
        if (exact is not null) return exact;
        var culture = await unit.Repository<PlantCulture>().Rows.OrderBy(culture => culture.Id)
            .FirstOrDefaultAsync(culture => culture.Name == values.Culture, cancellationToken).ConfigureAwait(false)
            ?? new PlantCulture { Name = values.Culture, Class = values.Class };
        var producer = await unit.Repository<Producer>().Rows.OrderBy(producer => producer.Id)
            .FirstOrDefaultAsync(producer => producer.Name == values.Producer, cancellationToken).ConfigureAwait(false)
            ?? new Producer { Name = values.Producer };
        var sorts = unit.Repository<PlantSort>().Rows.Include(sort => sort.Producer).OrderBy(sort => sort.Id);
        var sort = await sorts.FirstOrDefaultAsync(sort => sort.Name == values.Sort && sort.Producer.Name == values.Producer,
            cancellationToken).ConfigureAwait(false);
        if (sort is null)
        {
            sort = await sorts.FirstOrDefaultAsync(sort => sort.Name == values.Sort, cancellationToken).ConfigureAwait(false);
            if (sort is { } shared) shared.Producer = producer;
            else sort = new PlantSort { Name = values.Sort, Producer = producer };
        }
        return new Plant { PlantCulture = culture, PlantSort = sort };
    }
}
