using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.SeedEditing;

public sealed class DbSeedAcquisitionWriter(IUnitOfWorkFactory units) : ISeedAcquisitionWriter
{
    public async Task<Result<AcquisitionResultFromViewModel>> SaveAsync(SeedAcquisition acquisition, DateTime purchasedAt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(acquisition);
        await using var unit = units.Create();
        var seeds = unit.Repository<Seed>();
        var inventory = await seeds.Items.OrderBy(seed => seed.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var values = acquisition.Values;
        var plant = ResolvePlant(values, inventory);
        // Назначение производителя общему сорту предшествует поиску пакета, как в прежнем сценарии.
        var existing = inventory.FirstOrDefault(seed => seed.SeedsInfo.ExpirationDate.Year == values.BestBy.Year
            && seed.Plant.PlantSort.Producer.Name == values.Producer && seed.Plant.PlantCulture.Name == values.Culture
            && seed.Plant.PlantSort.Name == values.Sort);
        var package = existing is { } stored
            ? SeedPackage.Create(SeedPackageDbMapper.Read(stored.SeedsInfo)).Bind(current => acquisition.Replenish(current, purchasedAt))
            : acquisition.CreatePackage(purchasedAt);

        return await package.Match(async calculated =>
        {
            try
            {
                Seed target;
                if (existing is { } current)
                    target = current;
                else
                {
                    target = new Seed { Plant = plant, SeedsInfo = new() };
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
                return Result.Failure<AcquisitionResultFromViewModel>($"Ошибка БД: {error.InnerException?.Message ?? error.Message}");
            }
        }, error => Task.FromResult(Result.Failure<AcquisitionResultFromViewModel>(error))).ConfigureAwait(false);
    }

    private static Plant ResolvePlant(SeedAcquisitionValues values, IReadOnlyList<Seed> inventory)
    {
        var exact = inventory.FirstOrDefault(seed => seed.Plant.PlantCulture.Name == values.Culture
            && seed.Plant.PlantSort.Name == values.Sort && seed.Plant.PlantSort.Producer.Name == values.Producer);
        if (exact is { } matching) return matching.Plant;
        var culture = inventory.FirstOrDefault(seed => seed.Plant.PlantCulture.Name == values.Culture)?.Plant.PlantCulture
            ?? new PlantCulture { Name = values.Culture, Class = values.Class };
        var producer = inventory.FirstOrDefault(seed => seed.Plant.PlantSort.Producer.Name == values.Producer)?.Plant.PlantSort.Producer
            ?? new Producer { Name = values.Producer };
        var sort = inventory.FirstOrDefault(seed => seed.Plant.PlantSort.Name == values.Sort
            && seed.Plant.PlantSort.Producer.Name == values.Producer)?.Plant.PlantSort;
        if (sort is null)
        {
            sort = inventory.FirstOrDefault(seed => seed.Plant.PlantSort.Name == values.Sort)?.Plant.PlantSort;
            if (sort is { } shared) shared.Producer = producer;
            else sort = new PlantSort { Name = values.Sort, Producer = producer };
        }
        return new Plant { PlantCulture = culture, PlantSort = sort };
    }
}
