using BonfireDB.Context;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB;

class PlantSortRepository : DbRepository<PlantSort>
{
    public PlantSortRepository(DbBonfire db) : base(db) { }

    public override IQueryable<PlantSort> Items => base.Items
        .Include(item=>item.Producer)
    ;
}