using BonfireDB.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bonfire.Services.Interfaces
{
    public interface ISeedsService
    {
        IQueryable<Seed> Seeds { get; }

        Task<Seed> MakeASeed(Plant plant, SeedsInfo seedsInfo);
        Task<Seed> UpdateSeed(Seed seed);
        Task<Seed> DeleteSeed(Seed seed);
        Task<PlantSort> UpdateSort(PlantSort seed);

    }
}
