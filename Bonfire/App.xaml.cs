using Bonfire.Services;
using Bonfire.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Bonfire.Data;
using Bonfire.Infrastructure.Commands.Base;
using Bonfire.Services.Interfaces;

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

        SetupGlobalExceptionHandling();

        using (var scope = Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<DbInitializer>().InitializeAsync().Wait();
        }


        base.OnStartup(e);
        await host.StartAsync();
    }

    /// <summary>
    /// Единая точка обработки необработанных исключений: команды (через
    /// <see cref="CommandExceptionHandler"/>), UI-поток (Dispatcher), фоновые
    /// потоки (AppDomain) и неотслеженные задачи (TaskScheduler). Везде — лог + диалог,
    /// чтобы приложение не падало молча.
    /// </summary>
    private void SetupGlobalExceptionHandling()
    {
        var logger = Services.GetRequiredService<ILogger<App>>();
        var dialog = Services.GetRequiredService<IUserDialog>();

        void Handle(Exception ex, string source)
        {
            logger.LogError(ex, "Необработанное исключение ({Source})", source);
            dialog.Error(ex.Message, "Ошибка");
        }

        CommandExceptionHandler.OnException = ex => Handle(ex, "Команда");

        DispatcherUnhandledException += (_, args) =>
        {
            Handle(args.Exception, "UI-поток");
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                Dispatcher.Invoke(() => Handle(ex, "Фоновый поток"));
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Dispatcher.Invoke(() => Handle(args.Exception, "Задача"));
            args.SetObserved();
        };
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