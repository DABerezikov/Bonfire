using BonfireDB.Context;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB;

public class SeedlingsRepository(DbBonfire db) : DbRepository<Seedling>(db)
{
    public override IQueryable<Seedling> Items => base.Items
        .Include(item => item.Plant)
        .Include(item => item.Plant.PlantSort)
        .Include(item => item.Plant.PlantCulture)
        .Include(item => item.Plant.PlantSort.Producer)
        .Include(item => item.SeedlingInfos)
        .ThenInclude(item=> item.Replants)
            
    ;
}