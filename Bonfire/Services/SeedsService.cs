using Bonfire.Services.Interfaces;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using System.Linq;
using System.Threading.Tasks;

namespace Bonfire.Services
{
    

    internal class SeedsService(
        IRepository<Plant> plants,
        IRepository<Seed> seeds,
        IRepository<PlantSort> sort,
        IRepository<PlantCulture> culture,
        IRepository<Producer> producer,
        IRepository<SeedsInfo> info)
        : ISeedsService
    {
        public IQueryable<Seed> Seeds => seeds.Items;

        public async Task<Seed> MakeASeed(Plant plant, SeedsInfo seedsInfo)
        {
            if (plant.Id == 0)
            {
                if (plant.PlantCulture.Id==0)
                    plant.PlantCulture = await culture.AddAsync(plant.PlantCulture).ConfigureAwait(false);
                if (plant.PlantSort.Producer.Id==0)
                    plant.PlantSort.Producer = await producer.AddAsync(plant.PlantSort.Producer).ConfigureAwait(false);
                if (plant.PlantSort.Id == 0)
                    plant.PlantSort = await sort.AddAsync(plant.PlantSort).ConfigureAwait(false);
                plant = await plants.AddAsync(plant);
            }

            if (seedsInfo.Id == 0)
            {
                seedsInfo = await info.AddAsync(seedsInfo);
            }

            var seed = new Seed
            {
                Plant = plant,
                SeedsInfo = seedsInfo
            };
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
}
