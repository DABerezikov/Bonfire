using BonfireDB.Entities;
using BonfireDB.Entities.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BonfireDB.Planning.Libraries;

internal static class PlanningLibraryConfiguration
{
    internal static void Configure(ModelBuilder model)
    {
        var region = model.Entity<PlanningRegionRow>();
        region.ToTable("PlanningRegions");
        region.HasIndex(x => x.StableKey).IsUnique();
        region.OwnsOne(x => x.LastSpringFrost);
        region.OwnsOne(x => x.FirstAutumnFrost);
        region.HasData(new PlanningRegionRow { Id = 1, StableKey = "tomsk", Name = "Томск", TimeZoneId = "Asia/Tomsk" });

        var culture = model.Entity<CultureGrowingProfileRow>();
        culture.ToTable("PlanningCultureProfiles");
        culture.HasIndex(x => x.CultureId).IsUnique();
        culture.HasOne<PlantCulture>().WithMany().HasForeignKey(x => x.CultureId).OnDelete(DeleteBehavior.Restrict);
        culture.OwnsOne(x => x.Germination);
        culture.OwnsOne(x => x.SeedlingAge);
        culture.OwnsOne(x => x.Vegetation);
        culture.OwnsOne(x => x.Extras, ConfigureExtras);
        culture.Navigation(x => x.Extras).IsRequired();

        var sort = model.Entity<SortPlanningSettingsRow>();
        sort.ToTable("PlanningSortSettings");
        sort.HasIndex(x => x.SortId).IsUnique();
        sort.HasOne<PlantSort>().WithMany().HasForeignKey(x => x.SortId).OnDelete(DeleteBehavior.Restrict);
        sort.OwnsOne(x => x.Extras, ConfigureExtras);
        sort.Navigation(x => x.Extras).IsRequired();

        var window = model.Entity<RegionalPlantingWindowRow>();
        window.ToTable("PlanningRegionalWindows");
        window.HasIndex(x => new { x.RegionId, x.CultureId, x.Location, x.Placement, x.Purpose }).IsUnique();
        window.HasOne<PlanningRegionRow>().WithMany().HasForeignKey(x => x.RegionId).OnDelete(DeleteBehavior.Restrict);
        window.HasOne<PlantCulture>().WithMany().HasForeignKey(x => x.CultureId).OnDelete(DeleteBehavior.Restrict);
        window.OwnsOne(x => x.Window);
        window.Navigation(x => x.Window).IsRequired();
    }

    private static void ConfigureExtras<T>(OwnedNavigationBuilder<T, PlanningExtrasRow> extras) where T : class
    {
        extras.OwnsOne(x => x.PrickingOut);
        extras.OwnsOne(x => x.Hardening);
    }
}
