using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Context
{
    internal class DbBonfire : DbContext
    {
        public DbSet<Plant> Plants { get; set; }
        public DbSet<PlantCulture> PlantsCulture { get; set; }
        public  DbSet<PlantSort> PlantsSort { get; set; }
        public DbSet<SeedsInfo> SeedsInfo { get; set; }
        public DbSet<Seed> Seeds { get; set; }

        public DbBonfire(DbContextOptions<DbBonfire> options): base(options)
        {
            
        }

        
    }
}
