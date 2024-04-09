using BonfireDB.Context;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB;

public class SeedlingInfoRepository : DbRepository<SeedlingInfo>
{
    public SeedlingInfoRepository(DbBonfire db) : base(db) { }

    public override IQueryable<SeedlingInfo> Items => base.Items
        .Include(item => item.Replants)
        .Include(item => item.Treatments)
           

    ;
}