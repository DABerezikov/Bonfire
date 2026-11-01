using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Planning.Regions;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.Planning;
using BonfireDB.Planning.Libraries;
using BonfireDB.SeedEditing;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planning;

public sealed class DbPlanningContextReader(IUnitOfWorkFactory units) : IPlanningContextReader
{
    public Task<Result<ImmutableArray<PlanningChoiceFromViewModel>>> ReadChoicesAsync(CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var rows = await unit.Repository<Plant>().Rows.AsNoTracking()
                .Select(x => new { CultureId = x.PlantCulture.Id, CultureName = x.PlantCulture.Name, SortId = x.PlantSort.Id, SortName = x.PlantSort.Name })
                .Distinct().OrderBy(x => x.CultureName).ThenBy(x => x.SortName).ToArrayAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(rows.Select(x => new PlanningChoiceFromViewModel(x.CultureId, x.CultureName, x.SortId, x.SortName)).ToImmutableArray());
        }, "вариантов культур и сортов");

    public Task<Result<PlanningContextFromViewModel>> ReadAsync(RegionId regionId, CultureId cultureId, SortId sortId, CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var region = await unit.Repository<PlanningRegionRow>().Rows.AsNoTracking().SingleOrDefaultAsync(x => x.Id == regionId.Value, cancellationToken).ConfigureAwait(false);
            var culture = await unit.Repository<PlantCulture>().Rows.AsNoTracking().SingleOrDefaultAsync(x => x.Id == cultureId.Value, cancellationToken).ConfigureAwait(false);
            var sort = await unit.Repository<PlantSort>().Rows.AsNoTracking().SingleOrDefaultAsync(x => x.Id == sortId.Value, cancellationToken).ConfigureAwait(false);
            if (region is null || culture is null || sort is null)
                return Result.Failure<PlanningContextFromViewModel>("Регион, культура или сорт не найдены");
            if (!await unit.Repository<Plant>().Rows.AnyAsync(x => x.PlantCulture.Id == cultureId.Value && x.PlantSort.Id == sortId.Value, cancellationToken).ConfigureAwait(false))
                return Result.Failure<PlanningContextFromViewModel>("Сорт не связан с выбранной культурой");
            var profile = await unit.Repository<CultureGrowingProfileRow>().Rows.AsNoTracking().SingleOrDefaultAsync(x => x.CultureId == cultureId.Value, cancellationToken).ConfigureAwait(false);
            var settings = await unit.Repository<SortPlanningSettingsRow>().Rows.AsNoTracking().SingleOrDefaultAsync(x => x.SortId == sortId.Value, cancellationToken).ConfigureAwait(false);
            var windowRows = await unit.Repository<RegionalPlantingWindowRow>().Rows.AsNoTracking()
                .Where(x => x.RegionId == regionId.Value && x.CultureId == cultureId.Value).OrderBy(x => x.Id).ToArrayAsync(cancellationToken).ConfigureAwait(false);
            var windows = ImmutableArray.CreateBuilder<RegionalPlantingWindow>();
            foreach (var row in windowRows)
            {
                var parsed = PlanningLibraryDbMapper.Read(row);
                if (parsed.IsFailure) return parsed.Bind(_ => Result.Failure<PlanningContextFromViewModel>("Повреждён справочник сроков"));
                windows.Add(parsed.ValueOrThrow());
            }
            var cultureProfile = profile is null ? Result.Success(Option.None<CultureGrowingProfile>())
                : PlanningLibraryDbMapper.Read(profile).Bind(x => Result.Success(Option.Some(x)));
            var sortSettings = settings is null ? Result.Success(Option.None<SortPlanningSettings>())
                : PlanningLibraryDbMapper.Read(settings).Bind(x => Result.Success(Option.Some(x)));
            // Только данные, участвующие в расчёте/справке. Нет EF-графа семян или рассады.
            var json = JsonSerializer.Serialize(new { Region = region, Culture = new { culture.Id, culture.Name, culture.Class },
                Sort = new { sort.Id, sort.Name, sort.MinGerminationTime, sort.MaxGerminationTime,
                    sort.AgeOfSeedlings, sort.MaxAgeOfSeedlings, sort.GrowingSeason, sort.MaxGrowingSeason,
                    sort.LandingPattern, sort.LandingRowSpacing }, Profile = profile, Settings = settings, Windows = windowRows });
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
            return PlanningLibraryDbMapper.Read(region).Bind(regionSnapshot => cultureProfile.Bind(c => sortSettings.Bind(s =>
                Result.Success(new PlanningContextFromViewModel(regionSnapshot, new(culture.Id, culture.Name, culture.Class),
                    sort.Id, sort.Name, SortCharacteristicsDbMapper.Read(sort), c, s, windows.ToImmutable(), fingerprint)))));
        }, "контекста планирования");
}
