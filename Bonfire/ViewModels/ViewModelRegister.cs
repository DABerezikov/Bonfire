using Microsoft.Extensions.DependencyInjection;

namespace Bonfire.ViewModels
{
    internal static class ViewModelRegister
    {
        public static IServiceCollection AddViews(this IServiceCollection services) => services
           .AddTransient<MainWindowViewModel>()
           .AddTransient<SeedsViewModel>()
           
        ;
    }
}