using BonfireDB.Context;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB;

public class SeedbedsRepository : DbRepository<Seedbed>
{
    public SeedbedsRepository(DbBonfire db) : base(db) { }

    public override IQueryable<Seedbed> Items => base.Items
        .Include(item => item.Soil)
        .Include(item => item.Plantings)
        .ThenInclude(item=> item.PlantingInfo)
        .Include(item => item.Plantings)
        .ThenInclude(item=>item.Plant)
            

    ;
}