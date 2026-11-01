using Bonfire.Application.Garden;
using Bonfire.Application.Harvest;
using Bonfire.Application.Libraries;
using Bonfire.Application.Planning;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Application.Seedlings;
using Bonfire.Application.Seeds;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Entities.Planning;
using BonfireDB.Harvest;
using BonfireDB.Libraries;
using BonfireDB.Planning;
using BonfireDB.Planning.Libraries;
using BonfireDB.Planting;
using BonfireDB.SeedEditing;
using BonfireDB.Sowing;
using Microsoft.Extensions.DependencyInjection;

namespace BonfireDB;

public static class RepositoryRegister
{
    public static IServiceCollection AddRepositoriesInDb(this IServiceCollection services) => services
        // Фабрика единиц работы создаёт DI-область на операцию (короткий DbContext).
        .AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>()
        .AddSingleton<DbPlantingPlans>()
        .AddSingleton<IPlanningContextReader, DbPlanningContextReader>()
        .AddSingleton<IPlanningBedReader, DbPlanningBedReader>()
        .AddSingleton<DbPerennialPlanning>()
        .AddSingleton<IPerennialPlanningReader>(provider => provider.GetRequiredService<DbPerennialPlanning>())
        .AddSingleton<IPerennialPlanningWriter>(provider => provider.GetRequiredService<DbPerennialPlanning>())
        .AddScoped<IRepository<PerennialPlantingRow>, DbRepository<PerennialPlantingRow>>()
        .AddScoped<IRepository<PerennialSeasonRow>, DbRepository<PerennialSeasonRow>>()
        .AddSingleton<DbHarvestJournal>()
        .AddSingleton<IHarvestJournalReader>(provider => provider.GetRequiredService<DbHarvestJournal>())
        .AddSingleton<IHarvestJournalWriter>(provider => provider.GetRequiredService<DbHarvestJournal>())
        .AddSingleton<IHarvestSourceReader>(provider => provider.GetRequiredService<DbHarvestJournal>())
        .AddScoped<IRepository<HarvestEntryRow>, DbRepository<HarvestEntryRow>>()
        .AddSingleton<IPlantingPlanReader>(provider => provider.GetRequiredService<DbPlantingPlans>())
        .AddSingleton<IPlantingPlanWriter>(provider => provider.GetRequiredService<DbPlantingPlans>())
        .AddScoped<IRepository<SeasonalPlantingPlanRow>, DbRepository<SeasonalPlantingPlanRow>>()
        .AddScoped<IRepository<PlannedBatchRow>, DbRepository<PlannedBatchRow>>()
        .AddSingleton<DbPlanningLibraries>()
        .AddSingleton<IPlanningLibraryReader>(provider => provider.GetRequiredService<DbPlanningLibraries>())
        .AddSingleton<IPlanningLibraryWriter>(provider => provider.GetRequiredService<DbPlanningLibraries>())
        .AddScoped<IRepository<PlanningRegionRow>, DbRepository<PlanningRegionRow>>()
        .AddScoped<IRepository<CultureGrowingProfileRow>, DbRepository<CultureGrowingProfileRow>>()
        .AddScoped<IRepository<SortPlanningSettingsRow>, DbRepository<SortPlanningSettingsRow>>()
        .AddScoped<IRepository<RegionalPlantingWindowRow>, DbRepository<RegionalPlantingWindowRow>>()
        .AddSingleton<ISowingSessionFactory, DbSowingSessionFactory>()
        .AddSingleton<ISeedlingDeletionWriter, DbSeedlingDeletionWriter>()
        .AddSingleton<ISeedlingChangeWriter, DbSeedlingChangeWriter>()
        .AddSingleton<ISeedlingEditWriter, DbSeedlingEditWriter>()
        .AddSingleton<ISeedlingReader, DbSeedlingReader>()
        .AddSingleton<IPlantingWriter, DbPlantingWriter>()
        .AddSingleton<IGridWriter, DbGridWriter>()
        .AddSingleton<IGardenStateWriter, DbGardenStateWriter>()
        .AddSingleton<IGardenGeometryWriter, DbGardenGeometryWriter>()
        .AddSingleton<IElementPropertiesWriter, DbElementPropertiesWriter>()
        .AddSingleton<IGreenhousePropertiesWriter, DbGreenhousePropertiesWriter>()
        .AddSingleton<IGardenPropertiesWriter, DbGardenPropertiesWriter>()
        .AddSingleton<IGardenLockWriter, DbGardenLockWriter>()
        .AddSingleton<IElementDeletionWriter, DbElementDeletionWriter>()
        .AddSingleton<IGreenhouseDeletionWriter, DbGreenhouseDeletionWriter>()
        .AddSingleton<IGardenDeletionWriter, DbGardenDeletionWriter>()
        .AddSingleton<IGardenPlanDeletionWriter, DbGardenPlanDeletionWriter>()
        .AddSingleton<IGardenPlanCreationWriter, DbGardenPlanCreationWriter>()
        .AddSingleton<IGardenCreationWriter, DbGardenCreationWriter>()
        .AddSingleton<IGardenObjectCreationWriter, DbGardenObjectCreationWriter>()
        .AddSingleton<IGardenReader, DbGardenReader>()
        .AddSingleton<ILibraryReader, DbLibraryReader>()
        .AddSingleton<ICultureWriter, DbCultureWriter>()
        .AddSingleton<IProducerWriter, DbProducerWriter>()
        .AddSingleton<ISortWriter, DbSortWriter>()
        .AddSingleton<ISeedReader, DbSeedReader>()
        .AddSingleton<ISeedEditorOptionsReader, DbSeedEditorOptionsReader>()
        .AddSingleton<ISeedEditWriter, DbSeedEditWriter>()
        .AddSingleton<ISeedAcquisitionWriter, DbSeedAcquisitionWriter>()
        .AddSingleton<ISeedDeletionWriter, DbSeedDeletionWriter>()
        .AddScoped<IRepository<Plant>, PlantRepository>()
        .AddScoped<IRepository<PlantCulture>, DbRepository<PlantCulture>>()
        .AddScoped<IRepository<PlantSort>, PlantSortRepository>()
        .AddScoped<IRepository<SortOption>, DbRepository<SortOption>>()
        .AddScoped<IRepository<Producer>, DbRepository<Producer>>()
        .AddScoped<IRepository<SeedsInfo>, DbRepository<SeedsInfo>>()
        .AddScoped<IRepository<Seed>, SeedsRepository>()
        .AddScoped<IRepository<Seedling>, SeedlingsRepository>()
        .AddScoped<IRepository<SeedlingInfo>, SeedlingInfoRepository>()
        .AddScoped<IRepository<Replanting>, DbRepository<Replanting>>()
        .AddScoped<IRepository<Treatment>, DbRepository<Treatment>>()
        // Планировка огорода
        .AddScoped<IRepository<GardenPlan>, DbRepository<GardenPlan>>()
        .AddScoped<IRepository<Garden>, GardenRepository>()
        .AddScoped<IRepository<Greenhouse>, GreenhouseRepository>()
        .AddScoped<IRepository<GardenElement>, GardenElementRepository>()
        .AddScoped<IRepository<PlantingSpot>, DbRepository<PlantingSpot>>();
}