using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BonfireDB.Context;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bonfire.Data
{
    internal class DbInitializer
    {
        private readonly DbBonfire _Db;
        private readonly ILogger<DbInitializer> _Logger;

        public DbInitializer(DbBonfire db, ILogger<DbInitializer> logger)
        {
            _Db = db;
            _Logger = logger;
        }

        public async Task InitializeAsync()
        {
            var timer = Stopwatch.StartNew();
            _Logger.LogInformation("Инициализация БД...");
            //_Logger.LogInformation("Удаление существующей БД...");
            //await _db.Database.EnsureDeletedAsync().ConfigureAwait(false);
            //_Logger.LogInformation("Удаление существующей БД выполнено за {0} мс", timer.ElapsedMilliseconds);
            //_db.Database.EnsureCreated();
            _Logger.LogInformation("Миграция БД...");
            await _Db.Database.MigrateAsync().ConfigureAwait(false);
            _Logger.LogInformation("Миграция БД выполнена за {0} мс", timer.ElapsedMilliseconds);
            if (await _Db.Seeds.AnyAsync())
            {
                _Logger.LogInformation("Инициализация БД выполнена за {0} c", timer.Elapsed.Seconds);
                return;
            }

            //await InitializePlantCulture();
            //await InitializeProducers();
            //await InitializePlantSort();
            //await InitializePlant();
            //await InitializeSeedsInfo();
            //await InitializeSeeds();
            _Logger.LogInformation("Инициализация БД выполнена за {0} c", timer.Elapsed.Seconds);
        }

        private const int PlantCulturesCount = 20;
        private PlantCulture[] _PlantCultures;

        private async Task InitializePlantCulture()
        {
            var timer = Stopwatch.StartNew();
            var rnd = new Random();
            _Logger.LogInformation("Инициализация культур...");
            var classList = PlantClassList.GetClassList().ToArray();
            _PlantCultures = Enumerable.Range(1, PlantCulturesCount)
                .Select(i => new PlantCulture()
                {
                    Name = $"Культура {i}",
                    Class = rnd.NextItem(classList)
                }).ToArray();

            await _Db.PlantsCulture.AddRangeAsync(_PlantCultures);
            await _Db.SaveChangesAsync();
            _Logger.LogInformation("Инициализация культур выполнена за {0} мс", timer.ElapsedMilliseconds);
        }

        private const int ProducersCount = 20;
        private Producer[] _Producers;

        private async Task InitializeProducers()
        {
            var timer = Stopwatch.StartNew();
            _Logger.LogInformation("Инициализация производителей...");
            _Producers = Enumerable.Range(1, ProducersCount)
                .Select(i => new Producer()
                {
                    Name = $"Производитель {i}"
                    
                }).ToArray();

            await _Db.Producers.AddRangeAsync(_Producers);
            await _Db.SaveChangesAsync();
            _Logger.LogInformation("Инициализация производителей выполнена за {0} мс", timer.ElapsedMilliseconds);
        }

        private const int PlantSortsCount = 20;
        private PlantSort[] _PlantSorts;

        private async Task InitializePlantSort()
        {
            var rnd = new Random();
            var timer = Stopwatch.StartNew();
            _Logger.LogInformation("Инициализация сортов...");
            _PlantSorts = Enumerable.Range(1, PlantSortsCount)
                .Select(i => new PlantSort()
                {
                    Name = $"Сорт {i}",
                    Producer = rnd.NextItem((IList<Producer>)_Producers),
                    Description = $"Описание {i}",
                    AgeOfSeedlings = 10*i,
                    GrowingSeason = 15*i,
                    LandingPattern = 5*i,
                    MaxGerminationTime = 20*i,
                    MinGerminationTime = 10*i,
                    PlantColor = $"Цвет {i}",
                    PlantHeight = 4*i

                }).ToArray();

            await _Db.PlantsSort.AddRangeAsync(_PlantSorts);
            await _Db.SaveChangesAsync();
            _Logger.LogInformation("Инициализация сортов выполнена за {0} мс", timer.ElapsedMilliseconds);
        }

        private const int PlantsCount = 20;
        private Plant[] _Plants;

        private async Task InitializePlant()
        {
            var rnd = new Random();
            var timer = Stopwatch.StartNew();
            _Logger.LogInformation("Инициализация растений...");
            _Plants = Enumerable.Range(1, PlantsCount)
                .Select(_ => new Plant()
                {
                    //Name = $"Растение {i}",
                    PlantCulture = rnd.NextItem((IList<PlantCulture>)_PlantCultures),
                    PlantSort = rnd.NextItem((IList<PlantSort>)_PlantSorts)

                }).ToArray();

            await _Db.Plants.AddRangeAsync(_Plants);
            await _Db.SaveChangesAsync();
            _Logger.LogInformation("Инициализация растений выполнена за {0} мс", timer.ElapsedMilliseconds);
        }

        private const int SeedsInfoCount = 20;
        private SeedsInfo[] _SeedsInfo;

        private async Task InitializeSeedsInfo()
        {
            var timer = Stopwatch.StartNew();
            _Logger.LogInformation("Инициализация описания семян...");
            _SeedsInfo = Enumerable.Range(1, SeedsInfoCount)
                .Select(i => new SeedsInfo()
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

            await _Db.SeedsInfo.AddRangeAsync(_SeedsInfo);
            await _Db.SaveChangesAsync();
            _Logger.LogInformation("Инициализация описания семян выполнена за {0} мс", timer.ElapsedMilliseconds);
        }

        private const int SeedsCount = 20;
        private Seed[] _Seeds;

        private async Task InitializeSeeds()
        {
            var rnd = new Random();
            var timer = Stopwatch.StartNew();
            _Logger.LogInformation("Инициализация семян...");
            _Seeds = Enumerable.Range(1, SeedsCount)
                .Select(_ => new Seed()
                {
                    Plant = rnd.NextItem((IList<Plant>)_Plants),
                    SeedsInfo = rnd.NextItem((IList<SeedsInfo>)_SeedsInfo)

                }).ToArray();

            await _Db.Seeds.AddRangeAsync(_Seeds);
            await _Db.SaveChangesAsync();
            _Logger.LogInformation("Инициализация семян выполнена за {0} мс", timer.ElapsedMilliseconds);
        }
    }
}
