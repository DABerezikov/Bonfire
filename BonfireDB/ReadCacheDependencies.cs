using Bonfire.Application.Caching;
using BonfireDB.Entities;
using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Entities.Planning;
using Microsoft.EntityFrameworkCore.Metadata;

namespace BonfireDB;

internal static class ReadCacheDependencies
{
    public static ReadArea Areas(IEntityType entityType)
    {
        while (true)
        {
            var owner = entityType.FindOwnership();
            if (owner is not null)
            {
                entityType = owner.PrincipalEntityType;
                continue;
            }

            var type = entityType.ClrType;
            if (typeof(Plant).IsAssignableFrom(type) || typeof(PlantSort).IsAssignableFrom(type) || typeof(PlantCulture).IsAssignableFrom(type) || typeof(Producer).IsAssignableFrom(type)) return ReadArea.Catalog;
            if (typeof(SortOption).IsAssignableFrom(type)) return ReadArea.SortOptions;
            if (typeof(Seed).IsAssignableFrom(type) || typeof(SeedsInfo).IsAssignableFrom(type)) return ReadArea.Seeds;
            if (typeof(Seedling).IsAssignableFrom(type) || typeof(SeedlingInfo).IsAssignableFrom(type) || typeof(Treatment).IsAssignableFrom(type) || typeof(Replanting).IsAssignableFrom(type)) return ReadArea.Seedlings;
            if (typeof(GardenPlan).IsAssignableFrom(type) || typeof(GardenPlot).IsAssignableFrom(type) || typeof(GardenElement).IsAssignableFrom(type) || typeof(PlantingSpot).IsAssignableFrom(type)) return ReadArea.Garden;
            if (typeof(SeasonalPlantingPlanRow).IsAssignableFrom(type) || typeof(PlannedBatchRow).IsAssignableFrom(type)) return ReadArea.PlantingPlans;
            if (typeof(HarvestEntryRow).IsAssignableFrom(type)) return ReadArea.Harvest;
            if (typeof(PerennialPlantingRow).IsAssignableFrom(type) || typeof(PerennialSeasonRow).IsAssignableFrom(type)) return ReadArea.Perennials | ReadArea.PlantingPlans;
            if (typeof(PlanningRegionRow).IsAssignableFrom(type) || typeof(CultureGrowingProfileRow).IsAssignableFrom(type) || typeof(SortPlanningSettingsRow).IsAssignableFrom(type) || typeof(RegionalPlantingWindowRow).IsAssignableFrom(type)) return ReadArea.PlanningLibraries;
            return 0;
        }
    }
}
