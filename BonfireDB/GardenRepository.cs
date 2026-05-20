using BonfireDB.Context;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB;

class GardenRepository(DbBonfire db) : DbRepository<Garden>(db)
{
    public override IQueryable<Garden> Items => base.Items
        .Include(g => g.GardenPlan)
        .Include(g => g.Greenhouses)
            .ThenInclude(gh => gh.Elements)
                .ThenInclude(e => e.PlantingSpots)
        .Include(g => g.Elements)
            .ThenInclude(e => e.PlantingSpots)
                .ThenInclude(s => s.SeedlingInfo);
}
