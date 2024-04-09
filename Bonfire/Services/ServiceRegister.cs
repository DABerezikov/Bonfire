using Bonfire.Services.Interfaces;
using BonfireDB.Entities;
using Microsoft.Extensions.DependencyInjection;
using MoonCalendar;

namespace Bonfire.Services
{
    internal static class ServiceRegister
    {
        public static IServiceCollection AddServices(this IServiceCollection services) => services
            .AddSingleton<ISeedsService, SeedsService>()
            .AddSingleton<ISeedlingsService, SeedlingsService>()
            .AddSingleton<IUserDialog, UserDialog>()
            .AddSingleton(typeof(MoonPhase))
            .AddAutoMapper(cfg =>
            {
                cfg.CreateMap<Seed, Seed>();
                cfg.CreateMap<SeedsInfo, SeedsInfo>();
                cfg.CreateMap<Plant, Plant>();
                cfg.CreateMap<PlantCulture, PlantCulture>();
                cfg.CreateMap<PlantSort, PlantSort>();
                cfg.CreateMap<Producer, Producer>();
                cfg.CreateMap<Replanting, Replanting>();
                cfg.CreateMap<Treatment, Treatment>();
                cfg.CreateMap<Seedling, Seedling>();
                cfg.CreateMap<SeedlingInfo, SeedlingInfo>();
            });
    }
}
