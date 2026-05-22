namespace Repository.Tests;

// Репозиторий только отслеживает сущности; сохранение — за вызывающим (UoW).
// Поэтому тесты добавляют/меняют через репозиторий и затем явно вызывают SaveChanges.
public class DbRepositoryTests : IDisposable
{
    private readonly DbBonfire _db;

    public DbRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DbBonfire>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new DbBonfire(options);
    }

    public void Dispose() => _db.Dispose();

    private DbRepository<Producer> CreateRepo() => new(_db);

    // ── Add ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Add_NullItem_ThrowsArgumentNullException()
    {
        var repo = CreateRepo();
        Assert.Throws<ArgumentNullException>(() => repo.Add(null!));
    }

    [Fact]
    public void Add_DoesNotPersistUntilSaveChanges()
    {
        var repo = CreateRepo();
        repo.Add(new Producer { Name = "Гавриш" });

        // Отслежено как Added, но в БД ещё нет — сохранение делает вызывающий.
        Assert.Equal(0, _db.Producers.Count());

        _db.SaveChanges();
        Assert.Equal(1, _db.Producers.Count());
    }

    [Fact]
    public void Add_ThenSave_PersistsToDb()
    {
        var repo = CreateRepo();
        repo.Add(new Producer { Name = "Гавриш" });
        _db.SaveChanges();

        Assert.Equal(1, _db.Producers.Count());
        Assert.Equal("Гавриш", _db.Producers.First().Name);
    }

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_NullItem_ThrowsArgumentNullException()
    {
        var repo = CreateRepo();
        await Assert.ThrowsAsync<ArgumentNullException>(() => repo.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_ThenSave_PersistsToDb()
    {
        var repo = CreateRepo();
        await repo.AddAsync(new Producer { Name = "Аэлита" });
        await _db.SaveChangesAsync();

        Assert.Equal(1, _db.Producers.Count());
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_NullItem_ThrowsArgumentNullException()
    {
        var repo = CreateRepo();
        Assert.Throws<ArgumentNullException>(() => repo.Update(null!));
    }

    [Fact]
    public void Update_ExistingItem_ThenSave_UpdatesInDb()
    {
        var repo = CreateRepo();
        var producer = new Producer { Name = "Старое" };
        repo.Add(producer);
        _db.SaveChanges();

        producer.Name = "Новое";
        repo.Update(producer);
        _db.SaveChanges();

        Assert.Equal("Новое", _db.Producers.First().Name);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_NullItem_ThrowsArgumentNullException()
    {
        var repo = CreateRepo();
        await Assert.ThrowsAsync<ArgumentNullException>(() => repo.UpdateAsync(null!));
    }

    // ── Remove ────────────────────────────────────────────────────────────────

    [Fact]
    public void Remove_ExistingItem_ThenSave_RemovesFromDb()
    {
        var repo = CreateRepo();
        var producer = new Producer { Name = "Удалить" };
        repo.Add(producer);
        _db.SaveChanges();
        var id = producer.Id;

        repo.Remove(id);
        _db.SaveChanges();

        Assert.Equal(0, _db.Producers.Count());
    }

    [Fact]
    public void Remove_NonExistentId_OnSave_ThrowsConcurrencyException()
    {
        var repo = CreateRepo();
        Assert.Throws<DbUpdateConcurrencyException>(() =>
        {
            repo.Remove(9999);
            _db.SaveChanges();
        });
    }

    // ── RemoveAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveAsync_ExistingItem_ThenSave_RemovesFromDb()
    {
        var repo = CreateRepo();
        var producer = new Producer { Name = "Удалить" };
        await repo.AddAsync(producer);
        await _db.SaveChangesAsync();
        var id = producer.Id;

        await repo.RemoveAsync(id);
        await _db.SaveChangesAsync();

        Assert.Equal(0, _db.Producers.Count());
    }

    // ── Get / GetAsync ────────────────────────────────────────────────────────

    [Fact]
    public void Get_ExistingId_ReturnsItem()
    {
        var repo = CreateRepo();
        var producer = new Producer { Name = "Найти" };
        repo.Add(producer);
        _db.SaveChanges();

        var result = repo.Get(producer.Id);

        Assert.NotNull(result);
        Assert.Equal("Найти", result!.Name);
    }

    [Fact]
    public void Get_NonExistentId_ReturnsNull()
    {
        var repo = CreateRepo();
        var result = repo.Get(9999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ExistingId_ReturnsItem()
    {
        var repo = CreateRepo();
        var producer = new Producer { Name = "Найти Async" };
        await repo.AddAsync(producer);
        await _db.SaveChangesAsync();

        var result = await repo.GetAsync(producer.Id);

        Assert.NotNull(result);
        Assert.Equal("Найти Async", result!.Name);
    }

    [Fact]
    public async Task GetAsync_NonExistentId_ReturnsNull()
    {
        var repo = CreateRepo();
        var result = await repo.GetAsync(9999);
        Assert.Null(result);
    }

    // ── Items ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Items_AfterSave_ReturnsQueryable()
    {
        var repo = CreateRepo();
        repo.Add(new Producer { Name = "Первый" });
        repo.Add(new Producer { Name = "Второй" });
        _db.SaveChanges();

        Assert.Equal(2, repo.Items.Count());
    }
}
