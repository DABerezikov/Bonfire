using BonfireDB.Context;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB;

class PlantRepository :DbRepository<Plant>
{
    public PlantRepository(DbBonfire db) : base(db) { }

    public override IQueryable<Plant> Items => base.Items
        .Include(item=> item.PlantCulture)
        .Include(item=>item.PlantSort)
    ;
}