using Bonfire.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Bonfire.Services
{
    internal static class ServiceRegister
    {
        public static IServiceCollection AddServices(this IServiceCollection services) => services
           .AddTransient<ISeedsService, SeedsService>()
           .AddTransient<IUserDialog, UserDialog>()
        ;
    }
}
