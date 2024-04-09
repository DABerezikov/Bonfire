using BonfireDB.Context;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB;

public class DbRepository<T> : IRepository<T> where T : Entity, new()
{

    private readonly DbBonfire _Db;
    private readonly DbSet<T> _Set;
    public bool AutoSaveChanges { get; set; } = true;

    public DbRepository(DbBonfire db)
    {
        _Db=db;
        _Set = _Db.Set<T>();
    }

    public virtual IQueryable<T?> Items => _Set;

    public T Add(T item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        _Db.Entry(item).State = EntityState.Added;
        if (AutoSaveChanges)
            _Db.SaveChanges();
        return item;
    }

    public async Task<T> AddAsync(T item, CancellationToken cancel = default)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        _Db.Entry(item).State = EntityState.Added;
        if (AutoSaveChanges)
            await _Db.SaveChangesAsync(cancel).ConfigureAwait(false);
        return item;
    }

    public T? Get(int id) => Items.SingleOrDefault(item => item != null && item.Id == id);
   

    public async Task<T?> GetAsync(int id, CancellationToken cancel = default) => await Items
        .SingleOrDefaultAsync(item => item != null && item.Id == id, cancel)
        .ConfigureAwait(false);

    public void Remove(int id)
    {
        var item = _Set.Local.FirstOrDefault(i => i.Id == id) ?? new T { Id = id };
        _Db.Remove(item);
        if (AutoSaveChanges)
            _Db.SaveChanges();
    }

    public async Task RemoveAsync(int id, CancellationToken cancel = default)
    {
        var item = _Set.Local.FirstOrDefault(i => i.Id == id) ?? new T { Id = id };
        _Db.Remove(item);
        if (AutoSaveChanges)
            await _Db.SaveChangesAsync(cancel).ConfigureAwait(false);
    }

    public void Update(T item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        _Db.Entry(item).State = EntityState.Modified;
        if (AutoSaveChanges)
            _Db.SaveChanges();
    }

    public async Task UpdateAsync(T item, CancellationToken cancel = default)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        _Db.Entry(item).State = EntityState.Modified;
        if (AutoSaveChanges)
            await _Db.SaveChangesAsync(cancel).ConfigureAwait(false);
    }
}