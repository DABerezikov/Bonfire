using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.Extensions.DependencyInjection;

namespace BonfireDB;

public static class RepositoryRegister
{
    public static IServiceCollection AddRepositoriesInDb(this IServiceCollection services) => services
        .AddScoped<IRepository<Plant>, PlantRepository>()
        .AddScoped<IRepository<PlantCulture>, DbRepository<PlantCulture>>()
        .AddScoped<IRepository<PlantSort>, PlantSortRepository>()
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
        .AddScoped<IRepository<PlantingSpot>, DbRepository<PlantingSpot>>()
        ;

}