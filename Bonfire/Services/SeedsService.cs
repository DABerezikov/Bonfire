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

        public async Task<Seed> MakeASeed(string plantName, SeedsInfo seedsInfo)
        {
            
            var plant = await _plants.Items.FirstOrDefaultAsync(p=>p.Name== plantName);
            if (plant is null) return null;
            var seed = new Seed
            {
                Plant = plant,
                SeedsInfo = seedsInfo
            };
            return await _seeds.AddAsync(seed);

        }

    }
}
