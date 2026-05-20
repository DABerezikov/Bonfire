using BonfireDB.Context;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB;

// GardenElement — абстрактный: не удовлетворяет ограничению new() у DbRepository<T>.
// Реализуем IRepository<GardenElement> напрямую.
class GardenElementRepository(DbBonfire db) : IRepository<GardenElement>
{
    private readonly DbSet<GardenElement> _set = db.GardenElements;

    public bool AutoSaveChanges { get; set; } = true;

    public IQueryable<GardenElement> Items => _set
        .Include(e => e.Plot)
        .Include(e => e.PlantingSpots)
            .ThenInclude(s => s.SeedlingInfo);

    public GardenElement? Get(int id) => Items.SingleOrDefault(e => e.Id == id);

    public async Task<GardenElement?> GetAsync(int id, CancellationToken cancel = default)
        => await Items.SingleOrDefaultAsync(e => e.Id == id, cancel);

    public GardenElement Add(GardenElement item)
    {
        db.Entry(item).State = EntityState.Added;
        if (AutoSaveChanges) db.SaveChanges();
        return item;
    }

    public async Task<GardenElement> AddAsync(GardenElement item, CancellationToken cancel = default)
    {
        db.Entry(item).State = EntityState.Added;
        if (AutoSaveChanges) await db.SaveChangesAsync(cancel);
        return item;
    }

    public void Update(GardenElement item)
    {
        db.Entry(item).State = EntityState.Modified;
        if (AutoSaveChanges) db.SaveChanges();
    }

    public async Task UpdateAsync(GardenElement item, CancellationToken cancel = default)
    {
        db.Entry(item).State = EntityState.Modified;
        if (AutoSaveChanges) await db.SaveChangesAsync(cancel);
    }

    public void Remove(int id)
    {
        var item = _set.Local.FirstOrDefault(e => e.Id == id)
                   ?? _set.Find(id);
        if (item is not null) db.Remove(item);
        if (AutoSaveChanges) db.SaveChanges();
    }

    public async Task RemoveAsync(int id, CancellationToken cancel = default)
    {
        var item = _set.Local.FirstOrDefault(e => e.Id == id)
                   ?? await _set.FindAsync([id], cancel);
        if (item is not null) db.Remove(item);
        if (AutoSaveChanges) await db.SaveChangesAsync(cancel);
    }
}
