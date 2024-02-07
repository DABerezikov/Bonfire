using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.Extensions.DependencyInjection;

namespace BonfireDB;

public static class RepositoryRegister
{
    public static IServiceCollection AddRepositoriesInDb(this IServiceCollection services) => services
        .AddSingleton<IRepository<Plant>, PlantRepository>()
        .AddSingleton<IRepository<PlantCulture>, DbRepository<PlantCulture>>()
        .AddSingleton<IRepository<PlantSort>, PlantSortRepository>()
        .AddSingleton<IRepository<Producer>, DbRepository<Producer>>()
        .AddSingleton<IRepository<SeedsInfo>, DbRepository<SeedsInfo>>()
        .AddSingleton<IRepository<Seed>, SeedsRepository>()
        .AddSingleton<IRepository<Seedling>, SeedlingsRepository>()
        .AddSingleton<IRepository<SeedlingInfo>, DbRepository<SeedlingInfo>>()
        .AddSingleton<IRepository<Replanting>, DbRepository<Replanting>>()
        .AddSingleton<IRepository<Treatment>, DbRepository<Treatment>>()

        ;

}