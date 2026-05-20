using Bonfire.Services;
using Bonfire.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using Bonfire.Data;

namespace Bonfire;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static Window? ActiveWindow => Current.Windows.Cast<Window>().FirstOrDefault(w => w.IsActive);

    public static Window? FocusedWindow => Current.Windows.Cast<Window>().FirstOrDefault(w => w.IsFocused);

    public static IHost Host => field ??= Microsoft.Extensions.Hosting.Host
        .CreateDefaultBuilder(Environment.GetCommandLineArgs())
        .ConfigureAppConfiguration(cfg => cfg.AddJsonFile("appsettings.json", true, true))
        .ConfigureServices((host, services) => services
            .AddViews()
            .AddServices()
            .AddDatabase(host.Configuration.GetSection("Database"))
        )
        .Build();

    public static IServiceProvider Services => Host.Services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // WPF по умолчанию использует en-US для биндингов (десятичный разделитель ".").
        // Переопределяем Language всех FrameworkElement на текущую культуру системы,
        // чтобы биндинги double/decimal корректно работали с запятой.
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(
                XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        var host = Host;

        using (var scope = Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<DbInitializer>().InitializeAsync().Wait();
        }

           
        base.OnStartup(e);
        await host.StartAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        using var host = Host;
        await host.StopAsync();
    }

    internal static void ConfigureServices(HostBuilderContext host, IServiceCollection services) => services
        .AddViews()
        .AddServices()
        .AddDatabase(host.Configuration.GetSection("Database"))
    ;
}