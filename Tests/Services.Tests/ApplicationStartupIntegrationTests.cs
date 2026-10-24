using Bonfire;
using Bonfire.Infrastructure;
using BonfireDB.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading;

namespace Services.Tests;

public class ApplicationStartupIntegrationTests
{
    [Fact]
    public async Task Startup_RealHostWithTemporaryDatabase_MigratesAndDisposesScopes()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"bonfire-startup-{Guid.NewGuid():N}.db");
        try
        {
            var host = Program.CreateHostBuilder([]).ConfigureAppConfiguration((_, builder) =>
            {
                builder.Sources.Clear();
                builder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:Type"] = "SQLite",
                    ["Database:ConnectionStrings:SQLite"] = $"Data Source={databasePath};Pooling=False"
                });
            }).Build();
            await using (var lifetime = new ApplicationLifetime(host, token => Program.InitializeDatabaseAsync(host.Services, token)))
            {
                await lifetime.StartAsync();
                await using var scope = host.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<DbBonfire>();
                Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
                Assert.Equal(0, await db.Seedlings.CountAsync());
                Assert.Throws<InvalidOperationException>(() => host.Services.GetRequiredService<DbBonfire>());
            }
            Assert.Throws<ObjectDisposedException>(() => host.Services.CreateScope());
        }
        finally
        {
            File.Delete(databasePath);
            File.Delete(databasePath + "-wal");
            File.Delete(databasePath + "-shm");
        }
    }

    [Fact]
    public async Task DatabaseInitialization_PreCancelledToken_DoesNotCreateDatabase()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"bonfire-cancelled-{Guid.NewGuid():N}.db");
        try
        {
            using var provider = new ServiceCollection().AddLogging().AddDatabaseForTest(databasePath).BuildServiceProvider(
                new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Program.InitializeDatabaseAsync(provider, cancellation.Token));
            Assert.False(File.Exists(databasePath));
        }
        finally { File.Delete(databasePath); }
    }
}

internal static class StartupTestServices
{
    public static IServiceCollection AddDatabaseForTest(this IServiceCollection services, string databasePath)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Type"] = "SQLite", ["ConnectionStrings:SQLite"] = $"Data Source={databasePath};Pooling=False"
        }).Build();
        return Bonfire.Data.DbRegistrator.AddDatabase(services, configuration);
    }
}
