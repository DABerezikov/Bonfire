namespace Repository.Tests;

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
    public void Add_ValidItem_PersistsToDb()
    {
        var repo = CreateRepo();
        var producer = new Producer { Name = "Гавриш" };

        repo.Add(producer);

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
    public async Task AddAsync_ValidItem_PersistsToDb()
    {
        var repo = CreateRepo();
        var producer = new Producer { Name = "Аэлита" };

        await repo.AddAsync(producer);

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
    public void Update_ExistingItem_UpdatesInDb()
    {
        var repo = CreateRepo();
        var producer = new Producer { Name = "Старое" };
        repo.Add(producer);

        producer.Name = "Новое";
        repo.Update(producer);

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
    public void Remove_ExistingItem_RemovesFromDb()
    {
        var repo = CreateRepo();
        var producer = new Producer { Name = "Удалить" };
        repo.Add(producer);
        var id = producer.Id;

        repo.Remove(id);

        Assert.Equal(0, _db.Producers.Count());
    }

    [Fact]
    public void Remove_NonExistentId_ThrowsConcurrencyException()
    {
        var repo = CreateRepo();
        Assert.Throws<DbUpdateConcurrencyException>(() => repo.Remove(9999));
    }

    // ── RemoveAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveAsync_ExistingItem_RemovesFromDb()
    {
        var repo = CreateRepo();
        var producer = new Producer { Name = "Удалить" };
        await repo.AddAsync(producer);
        var id = producer.Id;

        await repo.RemoveAsync(id);

        Assert.Equal(0, _db.Producers.Count());
    }

    // ── Get / GetAsync ────────────────────────────────────────────────────────

    [Fact]
    public void Get_ExistingId_ReturnsItem()
    {
        var repo = CreateRepo();
        var producer = new Producer { Name = "Найти" };
        repo.Add(producer);

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

    // ── AutoSaveChanges ───────────────────────────────────────────────────────

    [Fact]
    public void AutoSaveChanges_False_DoesNotPersistImmediately()
    {
        var repo = CreateRepo();
        repo.AutoSaveChanges = false;

        var producer = new Producer { Name = "Не сохранять" };
        repo.Add(producer);

        // изменения отслеживаются EF, но SaveChanges не вызван
        // в InMemory-БД Added-записи доступны через ChangeTracker
        var tracked = _db.ChangeTracker.Entries<Producer>()
            .Any(e => e.State == EntityState.Added);
        Assert.True(tracked);
        // но через прямой запрос их нет (не saved)
        Assert.Equal(0, _db.Producers.AsNoTracking().Count());
    }

    [Fact]
    public void Items_ReturnsQueryable()
    {
        var repo = CreateRepo();
        repo.Add(new Producer { Name = "Первый" });
        repo.Add(new Producer { Name = "Второй" });

        Assert.Equal(2, repo.Items.Count());
    }
}
