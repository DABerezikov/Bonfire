using BonfireDB.Context;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB;

public class DbRepository<T> : IRepository<T> where T : Entity, new()
{

    private readonly DbBonfire _db;
    private readonly DbSet<T> _set;
    public bool AutoSaveChanges { get; set; } = true;

    public DbRepository(DbBonfire db)
    {
        _db=db;
        _set = _db.Set<T>();
    }

    public virtual IQueryable<T?> Items => _set;

    public T Add(T item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        _db.Entry(item).State = EntityState.Added;
        if (AutoSaveChanges)
            _db.SaveChanges();
        return item;
    }

    public async Task<T> AddAsync(T item, CancellationToken cancel = default)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        _db.Entry(item).State = EntityState.Added;
        if (AutoSaveChanges)
            await _db.SaveChangesAsync(cancel).ConfigureAwait(false);
        return item;
    }

    public T? Get(int id) => Items.SingleOrDefault(item => item != null && item.Id == id);
   

    public async Task<T?> GetAsync(int id, CancellationToken cancel = default) => await Items
        .SingleOrDefaultAsync(item => item != null && item.Id == id, cancel)
        .ConfigureAwait(false);

    public void Remove(int id)
    {
        var item = _set.Local.FirstOrDefault(i => i.Id == id) ?? new T { Id = id };
        _db.Remove(item);
        if (AutoSaveChanges)
            _db.SaveChanges();
    }

    public async Task RemoveAsync(int id, CancellationToken cancel = default)
    {
        var item = _set.Local.FirstOrDefault(i => i.Id == id) ?? new T { Id = id };
        _db.Remove(item);
        if (AutoSaveChanges)
            await _db.SaveChangesAsync(cancel).ConfigureAwait(false);
    }

    public void Update(T item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        _db.Entry(item).State = EntityState.Modified;
        if (AutoSaveChanges)
            _db.SaveChanges();
    }

    public async Task UpdateAsync(T item, CancellationToken cancel = default)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        _db.Entry(item).State = EntityState.Modified;
        if (AutoSaveChanges)
            await _db.SaveChangesAsync(cancel).ConfigureAwait(false);
    }
}