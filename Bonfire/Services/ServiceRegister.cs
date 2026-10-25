using Bonfire.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using MoonCalendar;
using Bonfire.Application.Seedlings;
using Bonfire.Infrastructure;
using Bonfire.Application.Libraries;

namespace Bonfire.Services
{
    internal static class ServiceRegister
    {
        public static IServiceCollection AddServices(this IServiceCollection services) => services
            .AddSingleton<ISeedsService, SeedsService>()
            .AddSingleton<ISeedlingsService, SeedlingsService>()
            .AddSingleton<ISowSeeds, SowSeeds>()
            .AddSingleton<ILunarCalendar, MoonPhaseCalendar>()
            .AddSingleton<IUserDialog, UserDialog>()
            .AddSingleton<IReportService, ReportService>()
            .AddSingleton(typeof(MoonPhase))
            .AddSingleton<IGardenService, GardenService>()
            .AddSingleton<IPlantingService, PlantingService>()
            .AddSingleton<ILibraryService, LibraryService>()
            .AddSingleton<IUpdateCulture, UpdateCulture>()
            .AddSingleton<IUpdateProducer, UpdateProducer>();
    }
}
