using BonfireDB.Context;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB;

class PlantSortRepository(DbBonfire db) : DbRepository<PlantSort>(db)
{
    public override IQueryable<PlantSort> Items => base.Items
        .Include(item=>item.Producer)
    ;
}