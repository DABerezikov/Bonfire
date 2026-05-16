using BonfireDB.Context;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB;

class PlantRepository(DbBonfire db) : DbRepository<Plant>(db)
{
    public override IQueryable<Plant> Items => base.Items
        .Include(item=> item.PlantCulture)
        .Include(item=>item.PlantSort)
        .Include(item => item.PlantSort.Producer)
    ;
}