using Bonfire.Services;
using Bonfire.ViewModels;
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
using Bonfire.Infrastructure;
using Bonfire.Infrastructure.Commands.Base;
using Bonfire.Services.Interfaces;

namespace Bonfire;

public partial class App : System.Windows.Application
{
    private readonly Lazy<ApplicationLifetime> _lifetime = new(() =>
        new ApplicationLifetime(Host, token => Program.InitializeDatabaseAsync(Services, token)));
    private Action<Exception>? _previousCommandHandler;
    private Action<Exception>? _commandHandler;
    private bool _errorHandlersInstalled;

    public static Window? ActiveWindow => Current.Windows.Cast<Window>().FirstOrDefault(w => w.IsActive);
    public static Window? FocusedWindow => Current.Windows.Cast<Window>().FirstOrDefault(w => w.IsFocused);
    public static IHost Host => field ??= Program.CreateHostBuilder(Environment.GetCommandLineArgs()).Build();
    public static IServiceProvider Services => Host.Services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
            SetupGlobalExceptionHandling();
            base.OnStartup(e);
            await _lifetime.Value.StartAsync();

            var window = new MainWindow();
            MainWindow = window;
            var shutdown = new WindowShutdown(async () =>
            {
                window.IsEnabled = false;
                await CompleteExitAsync();
            }, window.CompleteShutdown, error =>
            {
                Console.Error.WriteLine(error);
                MessageBox.Show(window, error.Message, "Ошибка завершения", MessageBoxButton.OK, MessageBoxImage.Error);
            });
            window.Closing += async (_, args) => await shutdown.OnClosingAsync(args);
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            window.Show();
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            try { await CompleteExitAsync(); }
            catch (Exception cleanupError) { Console.Error.WriteLine(cleanupError); }
            MessageBox.Show(error.Message, "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    internal async Task CompleteExitAsync()
    {
        RemoveGlobalExceptionHandling();
        if (_lifetime.IsValueCreated) await _lifetime.Value.DisposeAsync().ConfigureAwait(false);
    }

    private void HandleException(Exception error, string source)
    {
        Services.GetRequiredService<ILogger<App>>().LogError(error, "Необработанное исключение ({Source})", source);
        Services.GetRequiredService<IUserDialog>().Error(error.Message, "Ошибка");
    }

    private void SetupGlobalExceptionHandling()
    {
        // Проверяем зависимости до подписки, чтобы ошибка сборки не оставила частичные обработчики.
        _ = Services.GetRequiredService<ILogger<App>>();
        _ = Services.GetRequiredService<IUserDialog>();
        _previousCommandHandler = CommandExceptionHandler.OnException;
        _commandHandler = error => HandleException(error, "Команда");
        CommandExceptionHandler.OnException = _commandHandler;
        DispatcherUnhandledException += OnDispatcherError;
        AppDomain.CurrentDomain.UnhandledException += OnDomainError;
        TaskScheduler.UnobservedTaskException += OnTaskError;
        _errorHandlersInstalled = true;
    }

    private void RemoveGlobalExceptionHandling()
    {
        if (!_errorHandlersInstalled) return;
        DispatcherUnhandledException -= OnDispatcherError;
        AppDomain.CurrentDomain.UnhandledException -= OnDomainError;
        TaskScheduler.UnobservedTaskException -= OnTaskError;
        if (CommandExceptionHandler.OnException == _commandHandler)
            CommandExceptionHandler.OnException = _previousCommandHandler;
        _errorHandlersInstalled = false;
    }

    private void OnDispatcherError(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        HandleException(args.Exception, "UI-поток");
        args.Handled = true;
    }

    private void OnDomainError(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is Exception error)
            Dispatcher.Invoke(() => HandleException(error, "Фоновый поток"));
    }

    private void OnTaskError(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        Dispatcher.Invoke(() => HandleException(args.Exception, "Задача"));
        args.SetObserved();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        RemoveGlobalExceptionHandling();
        base.OnExit(e);
    }

    internal static void ConfigureServices(HostBuilderContext host, IServiceCollection services) => services
        .AddViews()
        .AddServices()
        .AddDatabase(host.Configuration.GetSection("Database"));
}
