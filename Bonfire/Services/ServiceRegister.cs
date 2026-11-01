using Bonfire.Services.Reports;
using System;
using Bonfire.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using MoonCalendar;
using Bonfire.Application.Seedlings;
using Bonfire.Infrastructure;
using Bonfire.Application.Libraries;
using Bonfire.Application.Seeds;
using Bonfire.Application.Garden;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Application.Planning;

namespace Bonfire.Services
{
    internal static class ServiceRegister
    {
        public static IServiceCollection AddServices(this IServiceCollection services) => services
            .AddSingleton<ISeedsService, SeedsService>()
            .AddSingleton<ISeedlingsService, SeedlingsService>()
            .AddSingleton<ISowSeeds, SowSeeds>()
            .AddSingleton<IDeleteSeedling, DeleteSeedling>()
            .AddSingleton<IChangeSeedling, ChangeSeedling>()
            .AddSingleton<IEditSeedling, EditSeedling>()
            .AddSingleton<ILunarCalendar, MoonPhaseCalendar>()
            .AddSingleton<ILunarPhaseCalculator, AstronomyLunarPhaseCalculator>()
            .AddSingleton<ILunarTrendCalendar, LunarTrendCalendar>()
            .AddSingleton<IRecommendSowing, RecommendSowing>()
            .AddSingleton<ICalculatePlannedBatch, CalculatePlannedBatch>()
            .AddSingleton<IManagePlantingPlans, ManagePlantingPlans>()
            .AddSingleton<IPlanningService, PlanningService>()
            .AddSingleton<IUserDialog, UserDialog>()
            .AddSingleton<IReportService, ReportService>()
            .AddSingleton<IReportWorkbookBuilder, ReportWorkbookBuilder>()
            .AddSingleton<IReportPathPolicy, ReportPathPolicy>()
            .AddSingleton<IReportFileWriter, ReportFileWriter>()
            .AddSingleton<IGardenService, GardenService>()
            .AddSingleton<IPlantingService, PlantingService>()
            .AddSingleton<IPlantInSpot, PlantInSpot>()
            .AddSingleton<IResizeGrid, ResizeGrid>()
            .AddSingleton<IChangeGardenState, ChangeGardenState>()
            .AddSingleton<ISaveGardenGeometry, SaveGardenGeometry>()
            .AddSingleton<IEditElementProperties, EditElementProperties>()
            .AddSingleton<IEditGreenhouseProperties, EditGreenhouseProperties>()
            .AddSingleton<IEditGardenProperties, EditGardenProperties>()
            .AddSingleton<ISetGardenLock, SetGardenLock>()
            .AddSingleton<IDeleteGardenElement, DeleteGardenElement>()
            .AddSingleton<IDeleteGreenhouse, DeleteGreenhouse>()
            .AddSingleton<IDeleteGarden, DeleteGarden>()
            .AddSingleton<IDeleteGardenPlan, DeleteGardenPlan>()
            .AddSingleton<ICreateGardenPlan, CreateGardenPlan>()
            .AddSingleton<ICreateGarden, CreateGarden>()
            .AddSingleton<ICreateGardenObjects, CreateGardenObjects>()
            .AddSingleton<ILibraryService, LibraryService>()
            .AddSingleton<IUpdateCulture, UpdateCulture>()
            .AddSingleton<ISavePlanningLibraries, SavePlanningLibraries>()
            .AddSingleton<IUpdateProducer, UpdateProducer>()
            .AddSingleton<IUpdateSort, UpdateSort>()
            .AddSingleton<IEditSeed, EditSeed>()
            .AddSingleton<IAcquireSeeds, AcquireSeeds>()
            .AddSingleton<IDeleteSeed, DeleteSeed>()
            .AddSingleton(TimeProvider.System);
    }
}
