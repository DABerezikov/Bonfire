using Bonfire.Services.Interfaces;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using System.Linq;
using System.Threading.Tasks;

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
                if (plant.PlantCulture.Id==0)
                    plant.PlantCulture = await _culture.AddAsync(plant.PlantCulture).ConfigureAwait(false);
                if (plant.PlantSort.Producer.Id==0)
                    plant.PlantSort.Producer = await _producer.AddAsync(plant.PlantSort.Producer).ConfigureAwait(false);
                if (plant.PlantSort.Id == 0)
                    plant.PlantSort = await _sort.AddAsync(plant.PlantSort).ConfigureAwait(false);
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
        
        public async Task<Seed> DeleteSeed(Seed seed)
        {
            
           await _seeds.RemoveAsync(seed.Id);
           return seed;

        }

        public async Task<PlantSort> UpdateSort(PlantSort sort)
        {

            await _sort.UpdateAsync(sort);
            return sort;

        }

        public async Task<PlantCulture> UpdateCulture(PlantCulture culture)
        {

            await _culture.UpdateAsync(culture);
            return culture;

        }

        public async Task<Producer> UpdateProducer(Producer producer)
        {

            await _producer.UpdateAsync(producer);
            return producer;

        }
    }
}
