using BonfireDB.Context;
using BonfireDB.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB
{
    public class SeedlingsRepository:DbRepository<Seedling>
    {
        public SeedlingsRepository(DbBonfire db) : base(db) { }

        public override IQueryable<Seedling> Items => base.Items
            .Include(item => item.Plant)
            .Include(item => item.Plant.PlantSort)
            .Include(item => item.Plant.PlantCulture)
            .Include(item => item.Plant.PlantSort.Producer)
            .Include(item => item.SeedlingInfo)
            .Include(item=>item.SeedlingInfo.Treatments)
            .Include(item=>item.SeedlingInfo.Replants)
        ;
    }
}
