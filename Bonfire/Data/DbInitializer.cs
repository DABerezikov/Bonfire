﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BonfireDB.Context;
using BonfireDB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bonfire.Data
{
    internal class DbInitializer
    {
        private readonly DbBonfire _db;
        private readonly ILogger<DbInitializer> _Logger;

        public DbInitializer(DbBonfire db, ILogger<DbInitializer> logger)
        {
            _db = db;
            _Logger = logger;
        }

        public async Task InitialazeAsync()
        {
            var timer = Stopwatch.StartNew();
            _Logger.LogInformation("Инициализация БД...");
            _Logger.LogInformation("Удаление существующей БД...");
            await _db.Database.EnsureDeletedAsync().ConfigureAwait(false);
            _Logger.LogInformation("Удаление существующей БД выполнено за {0} мс", timer.ElapsedMilliseconds);
            //_db.Database.EnsureCreated();
            _Logger.LogInformation("Миграция БД...");
            await _db.Database.MigrateAsync().ConfigureAwait(false);
            _Logger.LogInformation("Миграция БД выполнена за {0} мс", timer.ElapsedMilliseconds);
            if (await _db.Seeds.AnyAsync()) return;

            await InitializePlantCulture();
            await InitializeProducers();
            await InitializePlantSort();
            await InitializePlant();
            _Logger.LogInformation("Инициализация БД выполнена за {0} c", timer.Elapsed.Seconds);
        }

        private const int __PlantCulturesCount = 10;
        private PlantCulture[] _PlantCultures;

        private async Task InitializePlantCulture()
        {
            var timer = Stopwatch.StartNew();
            _Logger.LogInformation("Инициализация культур...");
            _PlantCultures = Enumerable.Range(1, __PlantCulturesCount)
                .Select(i => new PlantCulture()
                {
                    Name = $"Культура {i}",
                    Class = $"Класс {i}"
                }).ToArray();

            await _db.PlantsCulture.AddRangeAsync(_PlantCultures);
            await _db.SaveChangesAsync();
            _Logger.LogInformation("Инициализация культур выполнена за {0} мс", timer.ElapsedMilliseconds);
        }

        private const int __ProducersCount = 10;
        private Producer[] _Producers;

        private async Task InitializeProducers()
        {
            var timer = Stopwatch.StartNew();
            _Logger.LogInformation("Инициализация производителей...");
            _Producers = Enumerable.Range(1, __ProducersCount)
                .Select(i => new Producer()
                {
                    Name = $"Производитель {i}"
                    
                }).ToArray();

            await _db.Producers.AddRangeAsync(_Producers);
            await _db.SaveChangesAsync();
            _Logger.LogInformation("Инициализация производителей выполнена за {0} мс", timer.ElapsedMilliseconds);
        }

        private const int __PlantSortsCount = 10;
        private PlantSort[] _PlantSorts;

        private async Task InitializePlantSort()
        {
            var rnd = new Random();
            var timer = Stopwatch.StartNew();
            _Logger.LogInformation("Инициализация сортов...");
            _PlantSorts = Enumerable.Range(1, __PlantSortsCount)
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

            await _db.PlantsSort.AddRangeAsync(_PlantSorts);
            await _db.SaveChangesAsync();
            _Logger.LogInformation("Инициализация сортов выполнена за {0} мс", timer.ElapsedMilliseconds);
        }

        private const int __PlantsCount = 10;
        private Plant[] _Plants;

        private async Task InitializePlant()
        {
            var rnd = new Random();
            var timer = Stopwatch.StartNew();
            _Logger.LogInformation("Инициализация растений...");
            _Plants = Enumerable.Range(1, __PlantsCount)
                .Select(i => new Plant()
                {
                    Name = $"Растение {i}",
                    PlantCulture = rnd.NextItem((IList<PlantCulture>)_PlantCultures),
                    PlantSort = rnd.NextItem((IList<PlantSort>)_PlantSorts)

                }).ToArray();

            await _db.Plants.AddRangeAsync(_Plants);
            await _db.SaveChangesAsync();
            _Logger.LogInformation("Инициализация растений выполнена за {0} мс", timer.ElapsedMilliseconds);
        }
    }
}