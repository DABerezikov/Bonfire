using BonfireDB.Context;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB;

class SeedsRepository : DbRepository<Seed>
{
    public SeedsRepository(DbBonfire db) : base(db) { }

    public override IQueryable<Seed> Items => base.Items
        .Include(item=>item.Plant)
        .Include(item=>item.SeedsInfo)
    ;
}