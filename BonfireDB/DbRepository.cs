using BonfireDB.Context;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB;

public class DbRepository<T> : IRepository<T> where T : Entity, new()
{
    private readonly DbBonfire _db;
    private readonly DbSet<T> _set;

    public DbRepository(DbBonfire db)
    {
        _db = db;
        _set = _db.Set<T>();
    }

    public virtual IQueryable<T> Items => _set;

    // Attach отслеживает граф по значению ключа: сущности с Id == 0 → Added,
    // существующие (Id != 0) → Unchanged. Это позволяет добавлять новый корень,
    // ссылающийся на уже существующие (отсоединённые) сущности, без их дублирования.
    public T Add(T item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        _db.Attach(item);
        return item;
    }

    public Task<T> AddAsync(T item, CancellationToken cancel = default) => Task.FromResult(Add(item));

    public T? Get(int id) => Items.SingleOrDefault(item => item.Id == id);

    public async Task<T?> GetAsync(int id, CancellationToken cancel = default) => await Items
        .SingleOrDefaultAsync(item => item.Id == id, cancel)
        .ConfigureAwait(false);

    public void Remove(int id)
    {
        var item = _set.Local.FirstOrDefault(i => i.Id == id) ?? new T { Id = id };
        _db.Remove(item);
    }

    public Task RemoveAsync(int id, CancellationToken cancel = default)
    {
        Remove(id);
        return Task.CompletedTask;
    }

    // Update помечает весь граф (key-set → Modified, key-unset → Added),
    // поэтому правки в навигационных свойствах отсоединённой сущности
    // (например SeedsInfo.AmountSeeds) сохраняются при коротком контексте.
    public void Update(T item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        _db.Update(item);
    }

    public Task UpdateAsync(T item, CancellationToken cancel = default)
    {
        Update(item);
        return Task.CompletedTask;
    }
}
