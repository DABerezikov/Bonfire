using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bonfire.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bonfire;

public class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var app = new App();
        try
        {
            app.InitializeComponent();
            Environment.ExitCode = app.Run();
        }
        finally
        {
            // Резервная очистка на границе процесса, когда цикл Dispatcher уже завершён.
            try { app.CompleteExitAsync().GetAwaiter().GetResult(); }
            catch (Exception error)
            {
                Console.Error.WriteLine(error);
                Environment.ExitCode = 1;
            }
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) => Host
        .CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(configuration => configuration.AddJsonFile(
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true, reloadOnChange: true))
        .UseDefaultServiceProvider((_, options) =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        })
        .ConfigureServices(App.ConfigureServices);

    internal static async Task InitializeDatabaseAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DbInitializer>()
            .InitializeAsync(cancellationToken).ConfigureAwait(false);
    }
}
