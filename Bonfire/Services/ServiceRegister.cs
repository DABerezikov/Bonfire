using Bonfire.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Bonfire.Services
{
    internal static class ServiceRegister
    {
        public static IServiceCollection AddServices(this IServiceCollection services) => services
           .AddSingleton<ISeedsService, SeedsService>()
           .AddSingleton<IUserDialog, UserDialog>()
        ;
    }
}
