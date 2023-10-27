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

        public IQueryable<Seed> Seeds => _seeds.Items;

        public SeedsService(IRepository<Plant> plants, IRepository<Seed> seeds)
        {
            _plants = plants;
            _seeds = seeds;
        }

        public async Task<Seed> MakeASeed(Plant plant, SeedsInfo seedsInfo)
        {
            if (plant.Id==0)
                plant = await _plants.AddAsync(plant).WaitAsync(TimeSpan.FromSeconds(1));
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
