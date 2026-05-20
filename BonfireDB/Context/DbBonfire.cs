using BonfireDB.Entities;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Context;

public class DbBonfire(DbContextOptions<DbBonfire> options) : DbContext(options)
{
    // --- Семена и рассада ---
    public DbSet<Plant> Plants { get; set; }
    public DbSet<PlantCulture> PlantsCulture { get; set; }
    public DbSet<PlantSort> PlantsSort { get; set; }
    public DbSet<SeedsInfo> SeedsInfo { get; set; }
    public DbSet<Seed> Seeds { get; set; }
    public DbSet<Producer> Producers { get; set; }
    public DbSet<Seedling> Seedlings { get; set; }
    public DbSet<SeedlingInfo> SeedlingInfos { get; set; }
    public DbSet<Treatment> Treatments { get; set; }
    public DbSet<Replanting> Replants { get; set; }

    // --- Планировка огорода ---
    public DbSet<GardenPlan> GardenPlans { get; set; }
    public DbSet<GardenPlot> GardenPlots { get; set; }
    public DbSet<Garden> Gardens { get; set; }
    public DbSet<Greenhouse> Greenhouses { get; set; }
    public DbSet<GardenElement> GardenElements { get; set; }
    public DbSet<Bed> Beds { get; set; }
    public DbSet<ColdFrame> ColdFrames { get; set; }
    public DbSet<FlowerBed> FlowerBeds { get; set; }
    public DbSet<OpenGroundArea> OpenGroundAreas { get; set; }
    public DbSet<PlantingSpot> PlantingSpots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- TPH: иерархия GardenPlot ---
        modelBuilder.Entity<GardenPlot>()
            .HasDiscriminator<string>("PlotType")
            .HasValue<Garden>("Garden")
            .HasValue<Greenhouse>("Greenhouse");

        // Garden → GardenPlan
        modelBuilder.Entity<Garden>()
            .HasOne(g => g.GardenPlan)
            .WithMany(p => p.Gardens)
            .HasForeignKey(g => g.GardenPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Greenhouse → родительский GardenPlot (Garden или другая Greenhouse)
        modelBuilder.Entity<Greenhouse>()
            .HasOne(g => g.ParentPlot)
            .WithMany(p => p.Greenhouses)
            .HasForeignKey(g => g.ParentPlotId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- TPH: иерархия GardenElement ---
        modelBuilder.Entity<GardenElement>()
            .HasDiscriminator<string>("ElementType")
            .HasValue<Bed>("Bed")
            .HasValue<ColdFrame>("ColdFrame")
            .HasValue<FlowerBed>("FlowerBed")
            .HasValue<OpenGroundArea>("OpenGroundArea");

        // GardenElement → GardenPlot
        modelBuilder.Entity<GardenElement>()
            .HasOne(e => e.Plot)
            .WithMany(p => p.Elements)
            .HasForeignKey(e => e.PlotId)
            .OnDelete(DeleteBehavior.Cascade);

        // PlantingSpot → GardenElement
        modelBuilder.Entity<PlantingSpot>()
            .HasOne(s => s.GardenElement)
            .WithMany(e => e.PlantingSpots)
            .HasForeignKey(s => s.GardenElementId)
            .OnDelete(DeleteBehavior.Cascade);

        // PlantingSpot → SeedlingInfo (необязательная связь)
        modelBuilder.Entity<PlantingSpot>()
            .HasOne(s => s.SeedlingInfo)
            .WithMany()
            .HasForeignKey(s => s.SeedlingInfoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
