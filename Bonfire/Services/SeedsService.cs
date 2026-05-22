using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bonfire.Models;
using Bonfire.Services.Interfaces;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace Bonfire.Services;

internal class SeedsService(IUnitOfWorkFactory uowFactory) : ISeedsService
{
    public async Task<IReadOnlyList<Seed>> GetAllSeedsAsync()
    {
        await using var uow = uowFactory.Create();
        return await uow.Repository<Seed>().Items.ToListAsync();
    }

    public async Task<Seed?> GetSeedAsync(int id)
    {
        await using var uow = uowFactory.Create();
        return await uow.Repository<Seed>().GetAsync(id);
    }

    public async Task<(Seed seed, bool isNew)> AddOrUpdateSeedAsync(AddSeedRequest r, IReadOnlyList<Seed> existingSeeds)
    {
        var plant = ResolveOrCreatePlant(r, existingSeeds);
        var quantity = r.SizeUnit != Units.GramsOption ? r.QuantityInPack : 0;
        var weight   = r.SizeUnit == Units.GramsOption  ? r.QuantityInPack : 0;

        // Обновление существующего пакета семян
        if (ExistsProducer(r, existingSeeds))
        {
            var existing = existingSeeds.FirstOrDefault(s =>
                s.SeedsInfo.ExpirationDate.Year == r.BestBy.Year
                && s.Plant.PlantSort.Producer.Name == r.Producer
                && s.Plant.PlantCulture.Name == r.Culture
                && s.Plant.PlantSort.Name == r.Sort);

            if (existing != null)
            {
                if (r.SizeUnit != Units.GramsOption)
                    existing.SeedsInfo.AmountSeeds += quantity * r.PackCount;
                else
                    existing.SeedsInfo.AmountSeedsWeight += weight * r.PackCount;

                existing.SeedsInfo.PurchaseDate = DateTime.Now;
                existing.SeedsInfo.Note = r.Note;
                existing.SeedsInfo.CostPack = r.CostPack;

                var updated = await UpdateSeed(existing);
                return (updated, false);
            }
        }

        // Новый пакет семян
        var seedsInfo = new SeedsInfo
        {
            ExpirationDate = r.BestBy,
            Note = r.Note,
            PurchaseDate = DateTime.Now,
            SeedSource = r.SeedSource,
            CostPack = r.CostPack
        };

        if (r.SizeUnit != Units.GramsOption)
        {
            seedsInfo.QuantityPack = quantity;
            seedsInfo.AmountSeeds  = quantity * r.PackCount;
        }
        else
        {
            seedsInfo.WeightPack        = weight;
            seedsInfo.AmountSeedsWeight = weight * r.PackCount;
        }

        // Граф нового семени строится несколькими Add в рамках ОДНОГО UoW.
        await using var uow = uowFactory.Create();
        var newSeed = await MakeASeed(uow, plant, seedsInfo);
        await uow.SaveChangesAsync();
        return (newSeed, true);
    }

    public async Task ReturnSeedsFromSeedling(int seedId, double quantity, double? weight)
    {
        await using var uow = uowFactory.Create();
        var seeds = uow.Repository<Seed>();
        var seed = await seeds.Items.FirstAsync(s => s.Id == seedId);
        seed.SeedsInfo.AmountSeeds       += quantity;
        seed.SeedsInfo.AmountSeedsWeight += weight;
        await seeds.UpdateAsync(seed);
        await uow.SaveChangesAsync();
    }

    private static bool ExistsProducer(AddSeedRequest r, IReadOnlyList<Seed> existing) =>
        existing.Any(s => s.Plant.PlantSort.Producer.Name == r.Producer);

    private static Plant ResolveOrCreatePlant(AddSeedRequest r, IReadOnlyList<Seed> existing)
    {
        // Точное совпадение culture+sort+producer — берём существующий Plant
        var exactMatch = existing.FirstOrDefault(s =>
            s.Plant.PlantCulture.Name == r.Culture
            && s.Plant.PlantSort.Name == r.Sort
            && s.Plant.PlantSort.Producer.Name == r.Producer);
        if (exactMatch != null)
            return exactMatch.Plant;

        var plantCulture = existing.FirstOrDefault(s => s.Plant.PlantCulture.Name == r.Culture)?.Plant.PlantCulture
                           ?? new PlantCulture { Name = r.Culture, Class = r.Class };

        var producerEntity = existing.FirstOrDefault(s => s.Plant.PlantSort.Producer.Name == r.Producer)?.Plant.PlantSort.Producer
                             ?? new Producer { Name = r.Producer };

        PlantSort? plantSort = null;
        var sortMatch = existing.FirstOrDefault(s =>
            s.Plant.PlantSort.Name == r.Sort && s.Plant.PlantSort.Producer.Name == r.Producer);
        if (sortMatch != null)
            plantSort = sortMatch.Plant.PlantSort;
        else
        {
            var sortAny = existing.FirstOrDefault(s => s.Plant.PlantSort.Name == r.Sort);
            if (sortAny != null)
            {
                plantSort = sortAny.Plant.PlantSort;
                plantSort.Producer = producerEntity;
            }
            else
                plantSort = new PlantSort { Name = r.Sort, Producer = producerEntity };
        }

        return new Plant { PlantCulture = plantCulture, PlantSort = plantSort };
    }

    // Строит граф нового семени в рамках переданного UoW (без SaveChanges — его делает вызывающий).
    internal async Task<Seed> MakeASeed(IUnitOfWork uow, Plant plant, SeedsInfo seedsInfo)
    {
        if (plant.Id == 0)
        {
            if (plant.PlantCulture.Id == 0)
                plant.PlantCulture = await uow.Repository<PlantCulture>().AddAsync(plant.PlantCulture).ConfigureAwait(false);
            if (plant.PlantSort.Producer.Id == 0)
                plant.PlantSort.Producer = await uow.Repository<Producer>().AddAsync(plant.PlantSort.Producer).ConfigureAwait(false);
            if (plant.PlantSort.Id == 0)
                plant.PlantSort = await uow.Repository<PlantSort>().AddAsync(plant.PlantSort).ConfigureAwait(false);
            plant = await uow.Repository<Plant>().AddAsync(plant);
        }

        if (seedsInfo.Id == 0)
            seedsInfo = await uow.Repository<SeedsInfo>().AddAsync(seedsInfo);

        var seed = new Seed { Plant = plant, SeedsInfo = seedsInfo };
        seed.SeedsInfo.Seed = seed;
        return await uow.Repository<Seed>().AddAsync(seed);
    }

    public async Task<Seed> UpdateSeed(Seed seed)
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<Seed>().UpdateAsync(seed);
        await uow.SaveChangesAsync();
        return seed;
    }

    public async Task<Seed> DeleteSeed(Seed seed)
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<Seed>().RemoveAsync(seed.Id);
        await uow.SaveChangesAsync();
        return seed;
    }

    public async Task<PlantSort> UpdateSort(PlantSort sort1)
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<PlantSort>().UpdateAsync(sort1);
        await uow.SaveChangesAsync();
        return sort1;
    }

    public async Task<PlantCulture> UpdateCulture(PlantCulture culture1)
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<PlantCulture>().UpdateAsync(culture1);
        await uow.SaveChangesAsync();
        return culture1;
    }

    public async Task<Producer> UpdateProducer(Producer producer1)
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<Producer>().UpdateAsync(producer1);
        await uow.SaveChangesAsync();
        return producer1;
    }
}
