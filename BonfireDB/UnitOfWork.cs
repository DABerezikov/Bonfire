using BonfireDB.Context;
using BonfireDB.Entities.Base;
using Microsoft.Extensions.DependencyInjection;

namespace BonfireDB;

// UoW = обёртка над DI-областью (scope). В области резолвятся scoped DbContext и
// scoped репозитории — все они делят один контекст. Закрытие области закрывает контекст.
internal sealed class UnitOfWork(IServiceScope scope) : IUnitOfWork
{
    public IRepository<T> Repository<T>() where T : class, IEntity
        => scope.ServiceProvider.GetRequiredService<IRepository<T>>();

    public Task<int> SaveChangesAsync(CancellationToken cancel = default)
        => scope.ServiceProvider.GetRequiredService<DbBonfire>().SaveChangesAsync(cancel);

    public async ValueTask DisposeAsync()
    {
        if (scope is IAsyncDisposable asyncScope)
            await asyncScope.DisposeAsync().ConfigureAwait(false);
        else
            scope.Dispose();
    }
}

internal sealed class UnitOfWorkFactory(IServiceScopeFactory scopeFactory) : IUnitOfWorkFactory
{
    public IUnitOfWork Create() => new UnitOfWork(scopeFactory.CreateScope());
}
