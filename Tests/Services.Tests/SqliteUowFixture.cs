using BonfireDB.Entities.GardenPlanning;
using BonfireDB;
using BonfireDB.Context;
using BonfireDB.Entities.Base;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Bonfire.Application.Seedlings;
using Bonfire.Infrastructure;
using BonfireDB.Sowing;
using MoonCalendar;
using Bonfire.Application.Libraries;
using BonfireDB.Libraries;
using Bonfire.Application.Seeds;
using BonfireDB.SeedEditing;
using Bonfire.Application.Garden;
using BonfireDB.Planting;

namespace Services.Tests;

// Реальный стек данных на SQLite in-memory: проверяет рантайм-поведение EF,
// которое моки не ловят — короткий DbContext на операцию (через IUnitOfWorkFactory),
// графы отсоединённых сущностей и реальные ограничения FK/PK.
internal sealed class SqliteUowFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public IUnitOfWorkFactory Factory { get; }
    public Bonfire.Application.Caching.IReadModelCache Cache => _provider.GetRequiredService<Bonfire.Application.Caching.IReadModelCache>();

    public GardenService CreateGardenService() => new( new ResizeGrid(new DbGridWriter(Factory)), new ChangeGardenState(new DbGardenStateWriter(Factory)), new SaveGardenGeometry(new DbGardenGeometryWriter(Factory)), new EditElementProperties(new DbElementPropertiesWriter(Factory)), new EditGreenhouseProperties(new DbGreenhousePropertiesWriter(Factory)), new EditGardenProperties(new DbGardenPropertiesWriter(Factory)), new SetGardenLock(new DbGardenLockWriter(Factory)), new DeleteGardenElement(new DbElementDeletionWriter(Factory)), new DeleteGreenhouse(new DbGreenhouseDeletionWriter(Factory)), new DeleteGarden(new DbGardenDeletionWriter(Factory)), new DeleteGardenPlan(new DbGardenPlanDeletionWriter(Factory)), new CreateGardenPlan(new DbGardenPlanCreationWriter(Factory), TimeProvider.System), new CreateGarden(new DbGardenCreationWriter(Factory)), new CreateGardenObjects(new DbGardenObjectCreationWriter(Factory)), new DbGardenReader(Factory), Cache);

    public SeedsService CreateSeedsService() => new( new EditSeed(new DbSeedEditWriter(Factory)), new AcquireSeeds(new DbSeedAcquisitionWriter(Factory), TimeProvider.System), new DeleteSeed(new DbSeedDeletionWriter(Factory)), new BonfireDB.SeedEditing.DbSeedReader(Factory), new DbSeedEditorOptionsReader(Factory), Cache);

    public LibraryService CreateLibraryService() => new(new DbLibraryReader(Factory), new UpdateCulture(new DbCultureWriter(Factory)), new UpdateProducer(new DbProducerWriter(Factory)), new UpdateSort(new DbSortWriter(Factory)), new DeleteLibraryEntry(new DbLibraryEntryDeletionWriter(Factory)), Cache);

    public SeedlingsService CreateSeedlingsService()
    {
        var lunar = new MoonPhaseCalendar(new AstronomyLunarPhaseCalculator());
        return new SeedlingsService(lunar, new SowSeeds(new DbSowingSessionFactory(Factory), lunar), new DeleteSeedling(new DbSeedlingDeletionWriter(Factory)), new ChangeSeedling(new DbSeedlingChangeWriter(Factory)), new EditSeedling(new DbSeedlingEditWriter(Factory), lunar), new DbSeedlingReader(Factory), Cache);
    }

    public SqliteUowFixture(Action<string>? commandLog = null)
    {
        // Соединение держим открытым — на нём живёт in-memory БД, общая для всех областей.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<DbBonfire>(o =>
        {
            o.UseSqlite(_connection);
            if (commandLog is not null) o.LogTo(commandLog, [Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuted]);
        });
        services.AddRepositoriesInDb();
        _provider = services.BuildServiceProvider();

        using (var scope = _provider.CreateScope())
            scope.ServiceProvider.GetRequiredService<DbBonfire>().Database.EnsureCreated();

        Factory = _provider.GetRequiredService<IUnitOfWorkFactory>();
    }

    public Task<GardenPlan> PreparePlanAsync(string name, int year, string? description = null)
        => PrepareAsync(new GardenPlan { Name = name, Year = year, Description = description });

    public Task<Garden> PrepareGardenAsync(int planId, string name, double width, double height)
        => PrepareAsync(new Garden { GardenPlanId = planId, Name = name, WidthMeters = width,
            HeightMeters = height, CanvasWidth = width * 150, CanvasHeight = height * 150 });

    public Task<T> PrepareAsync<T>(T entity) where T : class => QueryAsync(async db =>
    {
        db.Add(entity); await db.SaveChangesAsync(); return entity;
    });

    // Только arrange/assert тестов: независимое чтение сохранённого EF-графа.
    public async Task<T?> LoadAsync<T>(int id) where T : class, IEntity
    {
        await using var unit = Factory.Create();
        return await unit.Repository<T>().GetAsync(id);
    }

    public async Task<IReadOnlyList<T>> LoadAllAsync<T>() where T : class, IEntity
    {
        await using var unit = Factory.Create();
        return await unit.Repository<T>().Items.ToListAsync();
    }

    // Действие в отдельном свежем контексте — для подготовки данных и проверок,
    // чтобы они не делили ChangeTracker с проверяемой операцией.
    public async Task<T> QueryAsync<T>(Func<DbBonfire, Task<T>> func)
    {
        using var scope = _provider.CreateScope();
        return await func(scope.ServiceProvider.GetRequiredService<DbBonfire>());
    }

    public async Task ExecuteAsync(Func<DbBonfire, Task> action)
    {
        using var scope = _provider.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<DbBonfire>());
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
