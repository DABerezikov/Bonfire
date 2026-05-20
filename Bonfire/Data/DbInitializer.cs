using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BonfireDB.Context;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bonfire.Data;

internal class DbInitializer(DbBonfire db, ILogger<DbInitializer> logger)
{
    public async Task InitializeAsync()
    {
        var timer = Stopwatch.StartNew();
        logger.LogInformation("Инициализация БД...");
        //_Logger.LogInformation("Удаление существующей БД...");
        //await _db.Database.EnsureDeletedAsync().ConfigureAwait(false);
        //_Logger.LogInformation("Удаление существующей БД выполнено за {0} мс", timer.ElapsedMilliseconds);
        //_db.Database.EnsureCreated();
        logger.LogInformation("Миграция БД...");
        await db.Database.MigrateAsync().ConfigureAwait(false);
        logger.LogInformation("Миграция БД выполнена за {0} мс", timer.ElapsedMilliseconds);
        await MigrateSeedlingMetadataAsync().ConfigureAwait(false);
        if (await db.Seeds.AnyAsync())
        {
            logger.LogInformation("Инициализация БД выполнена за {0} c", timer.Elapsed.Seconds);
            return;
        }

        //await InitializePlantCulture();
        //await InitializeProducers();
        //await InitializePlantSort();
        //await InitializePlant();
        //await InitializeSeedsInfo();
        //await InitializeSeeds();
        logger.LogInformation("Инициализация БД выполнена за {0} c", timer.Elapsed.Seconds);
    }

    // Копирует метаданные партии из SeedlingInfo[SeedlingNumber=0] в поля Seedling.
    // Однократно выполняется для существующих данных до введения новых полей.
    private async Task MigrateSeedlingMetadataAsync()
    {
        var toMigrate = await db.Seedlings
            .Where(s => s.LandingDate == null)
            .Include(s => s.SeedlingInfos)
            .ToListAsync()
            .ConfigureAwait(false);

        if (toMigrate.Count == 0) return;

        foreach (var seedling in toMigrate)
        {
            var meta = seedling.SeedlingInfos.FirstOrDefault(i => i.SeedlingNumber == 0);
            if (meta == null) continue;
            if (meta.LandingDate > DateTime.MinValue) seedling.LandingDate = meta.LandingDate;
            seedling.LunarPhase     = meta.LunarPhase;
            seedling.SeedlingSource = meta.SeedlingSource;
            seedling.PlantPlace     = meta.PlantPlace;
        }

        await db.SaveChangesAsync().ConfigureAwait(false);
        logger.LogInformation("Миграция метаданных рассады: перенесено {0} записей", toMigrate.Count);
    }

    private const int PlantCulturesCount = 20;
    private PlantCulture[] _plantCultures = null!;

    private async Task InitializePlantCulture()
    {
        var timer = Stopwatch.StartNew();
        var rnd = new Random();
        logger.LogInformation("Инициализация культур...");
        var classList = PlantClassList.GetClassList().ToArray();
        _plantCultures = Enumerable.Range(1, PlantCulturesCount)
            .Select(i => new PlantCulture
            {
                Name = $"Культура {i}",
                Class = rnd.NextItem(classList)
            }).ToArray();

        await db.PlantsCulture.AddRangeAsync(_plantCultures);
        await db.SaveChangesAsync();
        logger.LogInformation("Инициализация культур выполнена за {0} мс", timer.ElapsedMilliseconds);
    }

    private const int ProducersCount = 20;
    private Producer[] _producers = null!;

    private async Task InitializeProducers()
    {
        var timer = Stopwatch.StartNew();
        logger.LogInformation("Инициализация производителей...");
        _producers = Enumerable.Range(1, ProducersCount)
            .Select(i => new Producer
            {
                Name = $"Производитель {i}"
                    
            }).ToArray();

        await db.Producers.AddRangeAsync(_producers);
        await db.SaveChangesAsync();
        logger.LogInformation("Инициализация производителей выполнена за {0} мс", timer.ElapsedMilliseconds);
    }

    private const int PlantSortsCount = 20;
    private PlantSort[] _plantSorts = null!;

    private async Task InitializePlantSort()
    {
        var rnd = new Random();
        var timer = Stopwatch.StartNew();
        logger.LogInformation("Инициализация сортов...");
        _plantSorts = Enumerable.Range(1, PlantSortsCount)
            .Select(i => new PlantSort
            {
                Name = $"Сорт {i}",
                Producer = rnd.NextItem((IList<Producer>)_producers),
                Description = $"Описание {i}",
                AgeOfSeedlings = 10*i,
                GrowingSeason = 15*i,
                LandingPattern = 5*i,
                MaxGerminationTime = 20*i,
                MinGerminationTime = 10*i,
                PlantColor = $"Цвет {i}",
                PlantHeight = 4*i

            }).ToArray();

        await db.PlantsSort.AddRangeAsync(_plantSorts);
        await db.SaveChangesAsync();
        logger.LogInformation("Инициализация сортов выполнена за {0} мс", timer.ElapsedMilliseconds);
    }

    private const int PlantsCount = 20;
    private Plant[] _plants = null!;

    private async Task InitializePlant()
    {
        var rnd = new Random();
        var timer = Stopwatch.StartNew();
        logger.LogInformation("Инициализация растений...");
        _plants = Enumerable.Range(1, PlantsCount)
            .Select(_ => new Plant
            {
                //Name = $"Растение {i}",
                PlantCulture = rnd.NextItem((IList<PlantCulture>)_plantCultures),
                PlantSort = rnd.NextItem((IList<PlantSort>)_plantSorts)

            }).ToArray();

        await db.Plants.AddRangeAsync(_plants);
        await db.SaveChangesAsync();
        logger.LogInformation("Инициализация растений выполнена за {0} мс", timer.ElapsedMilliseconds);
    }

    private const int SeedsInfoCount = 20;
    private SeedsInfo[] _seedsInfo = null!;

    private async Task InitializeSeedsInfo()
    {
        var timer = Stopwatch.StartNew();
        logger.LogInformation("Инициализация описания семян...");
        _seedsInfo = Enumerable.Range(1, SeedsInfoCount)
            .Select(i => new SeedsInfo
            {
                WeightPack = i,
                AmountSeedsWeight = 10*i,
                AmountSeeds = i,
                CostPack = 100*i,
                ExpirationDate = DateTime.Now + TimeSpan.FromDays(730),
                Note = $"Примечание {i}",
                PurchaseDate = DateTime.Now,
                QuantityPack = 5*i,
                SeedSource = "Куплено"

            }).ToArray();

        await db.SeedsInfo.AddRangeAsync(_seedsInfo);
        await db.SaveChangesAsync();
        logger.LogInformation("Инициализация описания семян выполнена за {0} мс", timer.ElapsedMilliseconds);
    }

    private const int SeedsCount = 20;
    private Seed[] _seeds = null!;

    private async Task InitializeSeeds()
    {
        var rnd = new Random();
        var timer = Stopwatch.StartNew();
        logger.LogInformation("Инициализация семян...");
        _seeds = Enumerable.Range(1, SeedsCount)
            .Select(_ => new Seed
            {
                Plant = rnd.NextItem((IList<Plant>)_plants),
                SeedsInfo = rnd.NextItem((IList<SeedsInfo>)_seedsInfo)

            }).ToArray();

        await db.Seeds.AddRangeAsync(_seeds);
        await db.SaveChangesAsync();
        logger.LogInformation("Инициализация семян выполнена за {0} мс", timer.ElapsedMilliseconds);
    }
}