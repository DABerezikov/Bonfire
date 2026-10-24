using Bonfire.Data;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels;
using BonfireDB.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Services.Tests;

public class CompositionBehaviorTests
{
    [Fact]
    public void CompositionRoot_ResolvesAllScreensAndBusinessServices()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Type"] = "SQLite",
            ["Database:ConnectionStrings:SQLite"] = "Data Source=:memory:"
        }).Build();
        using var provider = new ServiceCollection().AddLogging()
            .AddDatabase(configuration.GetSection("Database")).AddServices().AddViews().BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<MainWindowViewModel>());
        Assert.NotNull(provider.GetRequiredService<ISeedsService>());
        Assert.NotNull(provider.GetRequiredService<ISeedlingsService>());
        Assert.NotNull(provider.GetRequiredService<IGardenService>());
        Assert.NotNull(provider.GetRequiredService<IPlantingService>());
        Assert.NotNull(provider.GetRequiredService<ILibraryService>());
        Assert.NotNull(provider.GetRequiredService<IReportService>());
        using var scope = provider.CreateScope();
        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", scope.ServiceProvider.GetRequiredService<DbBonfire>().Database.ProviderName);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DbInitializer>());
    }

    [Theory]
    [InlineData(null, "Не определён тип БД")]
    [InlineData("Unknown", "не поддерживается")]
    public void InvalidDatabaseType_IsRejectedWithActionableMessage(string? databaseType, string expected)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Type"] = databaseType }).Build();
        using var provider = new ServiceCollection().AddDatabase(configuration).BuildServiceProvider();
        using var scope = provider.CreateScope();
        var error = Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<DbBonfire>());
        Assert.Contains(expected, error.Message);
    }
}
