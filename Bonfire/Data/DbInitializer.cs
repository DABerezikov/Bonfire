using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BonfireDB.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bonfire.Data;

internal class DbInitializer(DbBonfire db, ILogger<DbInitializer> logger)
{
    public async Task InitializeAsync()
    {
        var timer = Stopwatch.StartNew();
        logger.LogInformation("Инициализация БД...");
        logger.LogInformation("Миграция БД...");
        await db.Database.MigrateAsync().ConfigureAwait(false);
        logger.LogInformation("Миграция БД выполнена за {0} мс", timer.ElapsedMilliseconds);
        await MigrateSeedlingMetadataAsync().ConfigureAwait(false);
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
}
