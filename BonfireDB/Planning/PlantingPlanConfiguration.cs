using BonfireDB.Entities;
using BonfireDB.Entities.Planning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planning;

internal static class PlantingPlanConfiguration
{
    internal static void Configure(ModelBuilder model)
    {
        var plan = model.Entity<SeasonalPlantingPlanRow>();
        plan.ToTable("SeasonalPlantingPlans");
        plan.Property(x => x.Version).IsConcurrencyToken();
        plan.HasIndex(x => x.SeasonYear);
        plan.HasOne<PlanningRegionRow>().WithMany().HasForeignKey(x => x.RegionId).OnDelete(DeleteBehavior.SetNull);
        var batch = model.Entity<PlannedBatchRow>();
        batch.ToTable("PlannedBatches");
        batch.Property(x => x.Version).IsConcurrencyToken();
        batch.HasOne(x => x.Plan).WithMany(x => x.Batches).HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
        batch.HasOne<PlantCulture>().WithMany().HasForeignKey(x => x.CultureId).OnDelete(DeleteBehavior.SetNull);
        batch.HasOne<PlantSort>().WithMany().HasForeignKey(x => x.SortId).OnDelete(DeleteBehavior.SetNull);
        batch.HasIndex(x => new { x.PlanId, x.Ordinal });
        batch.HasIndex(x => new { x.CultureId, x.SortId, x.Location, x.Placement });
        batch.HasIndex(x => x.SelectedSowingDate);
    }
}