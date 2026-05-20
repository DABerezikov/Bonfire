using BonfireDB.Context;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB;

class GreenhouseRepository(DbBonfire db) : DbRepository<Greenhouse>(db)
{
    public override IQueryable<Greenhouse> Items => base.Items
        .Include(g => g.ParentPlot)
        .Include(g => g.Elements)
            .ThenInclude(e => e.PlantingSpots)
                .ThenInclude(s => s.SeedlingInfo)
        .Include(g => g.Greenhouses);
}
