namespace BonfireDB.Entities.Base;

// Репозиторий только отслеживает сущности в DbContext своего Unit of Work.
// Add/Update/Remove НЕ сохраняют — единственный SaveChanges делает IUnitOfWork.
// Поэтому контекст живёт коротко (одна операция), а не всё время работы приложения.
public interface IRepository<T> where T : class, IEntity
{
    IQueryable<T> Items { get; }
    T? Get(int id);
    Task<T?> GetAsync(int id, CancellationToken cancel = default);
    T Add(T item);
    Task<T> AddAsync(T item, CancellationToken cancel = default);
    void Update(T item);
    Task UpdateAsync(T item, CancellationToken cancel = default);
    void Remove(int id);
    Task RemoveAsync(int id, CancellationToken cancel = default);
}
