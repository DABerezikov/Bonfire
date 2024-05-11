using System.Linq;
using System.Threading.Tasks;
using Bonfire.Services.Interfaces;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;

namespace Bonfire.Services
{

    internal class SeedbedsService(
        IRepository<Seedbed> seedbeds,
        IRepository<Soil> soils,
        IRepository<Treatment> treatments)
        : ISeedbedsService
    {
        public IQueryable<Seedbed> Seedbeds => seedbeds.Items;


        public async Task<Seedbed> MakeASeedbed(Seedbed seedbed)
        {

            if (seedbed.Soil.Id == 0)
                await soils.AddAsync(seedbed.Soil);
            
            return await seedbeds.AddAsync(seedbed);

        }

       
        public async Task<Seedbed> DeleteSeedbed(Seedbed seedbed)
        {

            await seedbeds.RemoveAsync(seedbed.Id);
            return seedbed;

        }

    }



}

