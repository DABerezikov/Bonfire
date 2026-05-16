using BonfireDB.Context;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB;

public class SeedlingInfoRepository(DbBonfire db) : DbRepository<SeedlingInfo>(db)
{
    public override IQueryable<SeedlingInfo> Items => base.Items
        .Include(item => item.Replants)
        .Include(item => item.Treatments)
           

    ;
}