using System;
using Bonfire.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using MoonCalendar;
using Bonfire.Application.Seedlings;
using Bonfire.Infrastructure;
using Bonfire.Application.Libraries;
using Bonfire.Application.Seeds;

namespace Bonfire.Services
{
    internal static class ServiceRegister
    {
        public static IServiceCollection AddServices(this IServiceCollection services) => services
            .AddSingleton<ISeedsService, SeedsService>()
            .AddSingleton<ISeedlingsService, SeedlingsService>()
            .AddSingleton<ISowSeeds, SowSeeds>()
            .AddSingleton<IDeleteSeedling, DeleteSeedling>()
            .AddSingleton<ILunarCalendar, MoonPhaseCalendar>()
            .AddSingleton<IUserDialog, UserDialog>()
            .AddSingleton<IReportService, ReportService>()
            .AddSingleton(typeof(MoonPhase))
            .AddSingleton<IGardenService, GardenService>()
            .AddSingleton<IPlantingService, PlantingService>()
            .AddSingleton<ILibraryService, LibraryService>()
            .AddSingleton<IUpdateCulture, UpdateCulture>()
            .AddSingleton<IUpdateProducer, UpdateProducer>()
            .AddSingleton<IUpdateSort, UpdateSort>()
            .AddSingleton<IEditSeed, EditSeed>()
            .AddSingleton<IAcquireSeeds, AcquireSeeds>()
            .AddSingleton<IDeleteSeed, DeleteSeed>()
            .AddSingleton(TimeProvider.System);
    }
}
