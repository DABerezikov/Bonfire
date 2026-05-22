using BonfireDB;
using BonfireDB.Context;
using BonfireDB.Entities.Base;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Services.Tests;

// Реальный стек данных на SQLite in-memory: проверяет рантайм-поведение EF,
// которое моки не ловят — короткий DbContext на операцию (через IUnitOfWorkFactory),
// графы отсоединённых сущностей и реальные ограничения FK/PK.
internal sealed class SqliteUowFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public IUnitOfWorkFactory Factory { get; }

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
