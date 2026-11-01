using BonfireDB.Entities;
using BonfireDB.Entities.Planning;
using Microsoft.EntityFrameworkCore;
namespace BonfireDB.Planning;
internal static class PerennialPlanningConfiguration
{
    internal static void Configure(ModelBuilder model)
    {
        var planting = model.Entity<PerennialPlantingRow>(); planting.ToTable("PerennialPlantings");
        planting.Property(x => x.Version).IsConcurrencyToken(); planting.HasIndex(x => x.EstablishmentYear);
        planting.HasOne<PlantCulture>().WithMany().HasForeignKey(x => x.CultureId).OnDelete(DeleteBehavior.SetNull);
        planting.HasOne<PlantSort>().WithMany().HasForeignKey(x => x.SortId).OnDelete(DeleteBehavior.SetNull);
        planting.HasOne<PlanningRegionRow>().WithMany().HasForeignKey(x => x.RegionId).OnDelete(DeleteBehavior.SetNull);
        var season = model.Entity<PerennialSeasonRow>(); season.ToTable("PlannedPerennialSeasons");
        season.Property(x => x.Version).IsConcurrencyToken(); season.HasIndex(x => new { x.PlanId, x.PlantingId }).IsUnique();
        season.HasOne<SeasonalPlantingPlanRow>().WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Cascade);
        season.HasOne<PerennialPlantingRow>().WithMany().HasForeignKey(x => x.PlantingId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<HarvestEntryRow>().HasOne<PerennialPlantingRow>().WithMany().HasForeignKey(x => x.PerennialId).OnDelete(DeleteBehavior.SetNull);
    }
}
