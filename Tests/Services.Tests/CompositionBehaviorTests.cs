using Bonfire.Data;
using Bonfire.Services.Interfaces;
using Bonfire.ViewModels;
using BonfireDB.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Bonfire.Application.Seedlings;
using Bonfire.Application.Libraries;
using Bonfire.Application.Seeds;
using Bonfire.Application.Garden;

namespace Services.Tests;

public class CompositionBehaviorTests
{
    [Fact]
    public void CompositionRoot_StrictScopes_ResolvesInitializerOnlyInsideScope()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Type"] = "SQLite", ["ConnectionStrings:SQLite"] = "Data Source=:memory:"
        }).Build();
        using var provider = new ServiceCollection().AddLogging().AddDatabase(configuration)
            .AddServices().AddViews().BuildServiceProvider(new ServiceProviderOptions
            { ValidateScopes = true, ValidateOnBuild = true });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var initializer = first.ServiceProvider.GetRequiredService<DbInitializer>();
        Assert.Same(initializer, first.ServiceProvider.GetRequiredService<DbInitializer>());
        Assert.NotSame(initializer, second.ServiceProvider.GetRequiredService<DbInitializer>());
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<DbInitializer>());
        Assert.NotNull(provider.GetRequiredService<ISowSeeds>());
        Assert.NotNull(provider.GetRequiredService<IPlantInSpot>());
        Assert.NotNull(provider.GetRequiredService<IPlantingWriter>());
        Assert.NotNull(provider.GetRequiredService<IUpdateCulture>());
        Assert.NotNull(provider.GetRequiredService<ICultureWriter>());
        Assert.NotNull(provider.GetRequiredService<IUpdateProducer>());
        Assert.NotNull(provider.GetRequiredService<IProducerWriter>());
        Assert.NotNull(provider.GetRequiredService<IUpdateSort>());
        Assert.NotNull(provider.GetRequiredService<ISortWriter>());
        Assert.NotNull(provider.GetRequiredService<IEditSeed>());
        Assert.NotNull(provider.GetRequiredService<ISeedEditWriter>());
        Assert.NotNull(provider.GetRequiredService<IAcquireSeeds>());
        Assert.NotNull(provider.GetRequiredService<ISeedAcquisitionWriter>());
        Assert.NotNull(provider.GetRequiredService<IDeleteSeed>());
        Assert.NotNull(provider.GetRequiredService<ISeedDeletionWriter>());
        Assert.NotNull(provider.GetRequiredService<TimeProvider>());
        Assert.NotNull(provider.GetRequiredService<MainWindowViewModel>());
    }

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
        Assert.NotNull(provider.GetRequiredService<ISowSeeds>());
        Assert.NotNull(provider.GetRequiredService<ISowingSessionFactory>());
        Assert.NotNull(provider.GetRequiredService<ILunarCalendar>());
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
