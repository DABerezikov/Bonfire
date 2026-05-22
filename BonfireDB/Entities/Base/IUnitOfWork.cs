namespace BonfireDB.Entities.Base;

/// <summary>
/// Единица работы: владеет одним короткоживущим <c>DbContext</c> и набором
/// репозиториев поверх него. Все репозитории, полученные из одного UoW, делят
/// общий контекст, поэтому построение графа объектов через несколько Add и
/// единственный <see cref="SaveChangesAsync"/> работают согласованно.
/// Освобождение (<see cref="System.IAsyncDisposable.DisposeAsync"/>) закрывает контекст.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IRepository<T> Repository<T>() where T : class, IEntity;
    Task<int> SaveChangesAsync(CancellationToken cancel = default);
}

/// <summary>
/// Фабрика единиц работы. Каждый <see cref="Create"/> создаёт новый UoW
/// с собственным свежим <c>DbContext</c> — на одну бизнес-операцию.
/// </summary>
public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();
}
