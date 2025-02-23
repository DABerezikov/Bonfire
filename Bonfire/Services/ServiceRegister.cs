using Bonfire.Models;
using Bonfire.Services.Interfaces;
using BonfireDB.Entities;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MoonCalendar;

namespace Bonfire.Services
{
    internal static class ServiceRegister
    {
        public static IServiceCollection AddServices(this IServiceCollection services) => services
            .AddSingleton<ISeedsService, SeedsService>()
            .AddSingleton<ISeedlingsService, SeedlingsService>()
            .AddSingleton<ISeedbedsService, SeedbedsService>()
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
                cfg.CreateMap<Soil, Soil>();
                cfg.CreateMap<Seedbed, Seedbed>();
                cfg.CreateMap<Seedbed, SeedBedFromViewModel>()
                    .ForMember(b=> b.Position, o=> o.MapFrom(s=> new Point(s.AbscissaX, s.OrdinateY)));
                cfg.CreateMap<SeedBedFromViewModel, Seedbed>()
                    .ForMember(b=>b.AbscissaX, o=> o.MapFrom(b=>b.Position.X))
                    .ForMember(b=>b.OrdinateY, o=> o.MapFrom(b=>b.Position.Y))
                    ;

            });
    }
}
