using System;
using Bonfire.Services.Interfaces;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Bonfire.Services
{
    internal class SeedsService : ISeedsService
    {
        private readonly IRepository<Plant> _plants;
        private readonly IRepository<Seed> _seeds;
        private readonly IRepository<PlantSort> _sort;
        private readonly IRepository<PlantCulture> _culture;
        private readonly IRepository<Producer> _producer;
        private readonly IRepository<SeedsInfo> _seedsInfo;

        public IQueryable<Seed> Seeds => _seeds.Items;

        public SeedsService(IRepository<Plant> plants,
            IRepository<Seed> seeds,
            IRepository<PlantSort> sort,
            IRepository<PlantCulture> culture,
            IRepository<Producer> producer,
            IRepository<SeedsInfo> seedsInfo)
        {
            _plants = plants;
            _seeds = seeds;
            _sort = sort;
            _culture = culture;
            _producer = producer;
            _seedsInfo = seedsInfo;
        }

        public async Task<Seed> MakeASeed(Plant plant, SeedsInfo seedsInfo)
        {
            if (plant.Id == 0)
            {
                plant.PlantCulture = await _culture.AddAsync(plant.PlantCulture);
                plant.PlantSort.Producer = await _producer.AddAsync(plant.PlantSort.Producer);
                plant.PlantSort = await _sort.AddAsync(plant.PlantSort);
                plant = await _plants.AddAsync(plant);
            }

            if (seedsInfo.Id == 0)
            {
                seedsInfo = await _seedsInfo.AddAsync(seedsInfo);
            }

            var seed = new Seed
            {
                Plant = plant,
                SeedsInfo = seedsInfo
            };
            seed.SeedsInfo.Seed = seed;
            return await _seeds.AddAsync(seed);

        }

        public async Task<Seed> UpdateSeed(Seed seed)
        {
            
           await _seeds.UpdateAsync(seed);
           return seed;

        }

    }
}
