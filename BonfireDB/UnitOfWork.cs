using Bonfire.Application.Caching;
using Microsoft.EntityFrameworkCore;
using BonfireDB.Context;
using BonfireDB.Entities.Base;
using Microsoft.Extensions.DependencyInjection;

namespace BonfireDB;

// UoW = обёртка над DI-областью (scope). В области резолвятся scoped DbContext и
// scoped репозитории — все они делят один контекст. Закрытие области закрывает контекст.
internal sealed class UnitOfWork(IServiceScope scope, IReadModelCache cache) : IUnitOfWork
{
    public IRepository<T> Repository<T>() where T : class, IEntity
        => scope.ServiceProvider.GetRequiredService<IRepository<T>>();

    public async Task<int> SaveChangesAsync(CancellationToken cancel = default)
    {
        var context = scope.ServiceProvider.GetRequiredService<DbBonfire>();
        var areas = context.ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Aggregate((ReadArea)0, (result, entry) => result | ReadCacheDependencies.Areas(entry.Metadata));
        var changes = await context.SaveChangesAsync(cancel).ConfigureAwait(false);
        if (changes > 0 && areas != 0) cache.Invalidate(areas);
        return changes;
    }

    public async ValueTask DisposeAsync()
    {
        if (scope is IAsyncDisposable asyncScope)
            await asyncScope.DisposeAsync().ConfigureAwait(false);
        else
            scope.Dispose();
    }
}

internal sealed class UnitOfWorkFactory(IServiceScopeFactory scopeFactory, IReadModelCache cache) : IUnitOfWorkFactory
{
    public IUnitOfWork Create() => new UnitOfWork(scopeFactory.CreateScope(), cache);
}