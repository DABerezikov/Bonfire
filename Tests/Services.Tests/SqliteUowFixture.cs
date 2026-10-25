using BonfireDB;
using BonfireDB.Context;
using BonfireDB.Entities.Base;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Bonfire.Application.Seedlings;
using Bonfire.Infrastructure;
using BonfireDB.Sowing;
using MoonCalendar;
using Bonfire.Application.Libraries;
using BonfireDB.Libraries;
using Bonfire.Application.Seeds;
using BonfireDB.SeedEditing;

namespace Services.Tests;

// Реальный стек данных на SQLite in-memory: проверяет рантайм-поведение EF,
// которое моки не ловят — короткий DbContext на операцию (через IUnitOfWorkFactory),
// графы отсоединённых сущностей и реальные ограничения FK/PK.
internal sealed class SqliteUowFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public IUnitOfWorkFactory Factory { get; }

    public SeedsService CreateSeedsService() => new(Factory, new EditSeed(new DbSeedEditWriter(Factory)), new AcquireSeeds(new DbSeedAcquisitionWriter(Factory), TimeProvider.System), new DeleteSeed(new DbSeedDeletionWriter(Factory)));

    public LibraryService CreateLibraryService() => new(Factory, new UpdateCulture(new DbCultureWriter(Factory)), new UpdateProducer(new DbProducerWriter(Factory)), new UpdateSort(new DbSortWriter(Factory)));

    public SeedlingsService CreateSeedlingsService()
    {
        var lunar = new MoonPhase();
        return new SeedlingsService(Factory, lunar, new SowSeeds(new DbSowingSessionFactory(Factory), new MoonPhaseCalendar(lunar)), new DeleteSeedling(new DbSeedlingDeletionWriter(Factory)));
    }

    public SqliteUowFixture()
    {
        // Соединение держим открытым — на нём живёт in-memory БД, общая для всех областей.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<DbBonfire>(o => o.UseSqlite(_connection));
        services.AddRepositoriesInDb();
        _provider = services.BuildServiceProvider();

        using (var scope = _provider.CreateScope())
            scope.ServiceProvider.GetRequiredService<DbBonfire>().Database.EnsureCreated();

        Factory = _provider.GetRequiredService<IUnitOfWorkFactory>();
    }

    // Действие в отдельном свежем контексте — для подготовки данных и проверок,
    // чтобы они не делили ChangeTracker с проверяемой операцией.
    public async Task<T> QueryAsync<T>(Func<DbBonfire, Task<T>> func)
    {
        using var scope = _provider.CreateScope();
        return await func(scope.ServiceProvider.GetRequiredService<DbBonfire>());
    }

    public async Task ExecuteAsync(Func<DbBonfire, Task> action)
    {
        using var scope = _provider.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<DbBonfire>());
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
