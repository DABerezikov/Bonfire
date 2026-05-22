using BonfireDB.Context;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB;

// GardenElement — абстрактный: не удовлетворяет ограничению new() у DbRepository<T>.
// Реализуем IRepository<GardenElement> напрямую. Сохранение — за Unit of Work.
class GardenElementRepository(DbBonfire db) : IRepository<GardenElement>
{
    private readonly DbSet<GardenElement> _set = db.GardenElements;

    public IQueryable<GardenElement> Items => _set
        .Include(e => e.Plot)
        .Include(e => e.PlantingSpots)
            .ThenInclude(s => s.SeedlingInfo);

    public GardenElement? Get(int id) => Items.SingleOrDefault(e => e.Id == id);

    public async Task<GardenElement?> GetAsync(int id, CancellationToken cancel = default)
        => await Items.SingleOrDefaultAsync(e => e.Id == id, cancel);

    public GardenElement Add(GardenElement item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        db.Attach(item);
        return item;
    }

    public Task<GardenElement> AddAsync(GardenElement item, CancellationToken cancel = default)
        => Task.FromResult(Add(item));

    public void Update(GardenElement item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        db.Update(item);
    }

    public Task UpdateAsync(GardenElement item, CancellationToken cancel = default)
    {
        Update(item);
        return Task.CompletedTask;
    }

    public void Remove(int id)
    {
        var item = _set.Local.FirstOrDefault(e => e.Id == id) ?? _set.Find(id);
        if (item is not null) db.Remove(item);
    }

    public async Task RemoveAsync(int id, CancellationToken cancel = default)
    {
        var item = _set.Local.FirstOrDefault(e => e.Id == id)
                   ?? await _set.FindAsync([id], cancel);
        if (item is not null) db.Remove(item);
    }
}
