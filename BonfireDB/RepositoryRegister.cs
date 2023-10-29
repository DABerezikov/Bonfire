using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.Extensions.DependencyInjection;

namespace BonfireDB;

public static class RepositoryRegister
{
    public static IServiceCollection AddRepositoriesInDb(this IServiceCollection services) => services
        .AddTransient<IRepository<Plant>, PlantRepository>()
        .AddTransient<IRepository<PlantCulture>, DbRepository<PlantCulture>>()
        .AddTransient<IRepository<PlantSort>, PlantSortRepository>()
        .AddTransient<IRepository<Producer>, DbRepository<Producer>>()
        .AddTransient<IRepository<SeedsInfo>, DbRepository<SeedsInfo>>()
        .AddTransient<IRepository<Seed>, SeedsRepository>()
        
        ;

}