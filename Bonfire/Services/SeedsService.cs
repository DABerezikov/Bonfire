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

internal class SeedsService(
    IRepository<Plant> plants,
    IRepository<Seed> seeds,
    IRepository<PlantSort> sort,
    IRepository<PlantCulture> culture,
    IRepository<Producer> producer,
    IRepository<SeedsInfo> info)
    : ISeedsService
{
    public async Task<IReadOnlyList<Seed>> GetAllSeedsAsync() => await seeds.Items.ToListAsync();

    public async Task<Seed?> GetSeedAsync(int id) => await seeds.GetAsync(id);

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

        var newSeed = await MakeASeed(plant, seedsInfo);
        return (newSeed, true);
    }

    public async Task ReturnSeedsFromSeedling(int seedId, double quantity, double? weight)
    {
        var seed = await seeds.Items.FirstAsync(s => s.Id == seedId);
        seed.SeedsInfo.AmountSeeds       += quantity;
        seed.SeedsInfo.AmountSeedsWeight += weight;
        await seeds.UpdateAsync(seed);
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

    internal async Task<Seed> MakeASeed(Plant plant, SeedsInfo seedsInfo)
    {
        if (plant.Id == 0)
        {
            if (plant.PlantCulture.Id == 0)
                plant.PlantCulture = await culture.AddAsync(plant.PlantCulture).ConfigureAwait(false);
            if (plant.PlantSort.Producer.Id == 0)
                plant.PlantSort.Producer = await producer.AddAsync(plant.PlantSort.Producer).ConfigureAwait(false);
            if (plant.PlantSort.Id == 0)
                plant.PlantSort = await sort.AddAsync(plant.PlantSort).ConfigureAwait(false);
            plant = await plants.AddAsync(plant);
        }

        if (seedsInfo.Id == 0)
            seedsInfo = await info.AddAsync(seedsInfo);

        var seed = new Seed { Plant = plant, SeedsInfo = seedsInfo };
        seed.SeedsInfo.Seed = seed;
        return await seeds.AddAsync(seed);
    }

    public async Task<Seed> UpdateSeed(Seed seed)
    {
        await seeds.UpdateAsync(seed);
        return seed;
    }
        
    public async Task<Seed> DeleteSeed(Seed seed)
    {
            
        await seeds.RemoveAsync(seed.Id);
        return seed;

    }

    public async Task<PlantSort> UpdateSort(PlantSort sort1)
    {

        await sort.UpdateAsync(sort1);
        return sort1;

    }

    public async Task<PlantCulture> UpdateCulture(PlantCulture culture1)
    {

        await culture.UpdateAsync(culture1);
        return culture1;

    }

    public async Task<Producer> UpdateProducer(Producer producer1)
    {

        await producer.UpdateAsync(producer1);
        return producer1;

    }
}