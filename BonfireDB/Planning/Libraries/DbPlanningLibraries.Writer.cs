using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Planning.Regions;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.Planning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planning.Libraries;

public sealed partial class DbPlanningLibraries
{
    public Task<Result<RegionFromViewModel>> SaveRegionAsync(Option<RegionId> id, RegionDefinition definition, CancellationToken cancellationToken)
        => WriteAsync(async () =>
        {
            await using var unit = units.Create();
            var repository = unit.Repository<PlanningRegionRow>();
            var row = await id.Match(value => repository.GetAsync(value.Value, cancellationToken),
                () => Task.FromResult<PlanningRegionRow?>(new())).ConfigureAwait(false);
            if (row is null) return Result.Failure<RegionFromViewModel>("Регион не найден");
            row.Name = definition.Name.Value;
            row.TimeZoneId = definition.TimeZoneId;
            row.LastSpringFrost = PlanningLibraryDbMapper.Write(definition.Reference.LastSpringFrost);
            row.FirstAutumnFrost = PlanningLibraryDbMapper.Write(definition.Reference.FirstAutumnFrost);
            row.Source = definition.Reference.Source.ToNullable();
            row.Note = definition.Reference.Note.ToNullable();
            row.IsArchived = definition.IsArchived;
            if (id is Option<RegionId>.None) repository.Add(row);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return PlanningLibraryDbMapper.Read(row);
        });

    public Task<Result<CultureGrowingProfile>> SaveCultureProfileAsync(CultureGrowingProfile profile, CancellationToken cancellationToken)
        => WriteAsync(async () =>
        {
            await using var unit = units.Create();
            if (!await unit.Repository<PlantCulture>().Rows.AnyAsync(x => x.Id == profile.CultureId.Value, cancellationToken).ConfigureAwait(false))
                return Result.Failure<CultureGrowingProfile>("Культура не найдена");
            var repository = unit.Repository<CultureGrowingProfileRow>();
            var row = await repository.Rows.SingleOrDefaultAsync(x => x.CultureId == profile.CultureId.Value, cancellationToken).ConfigureAwait(false);
            if (row is null)
            {
                row = new() { CultureId = profile.CultureId.Value };
                repository.Add(row);
            }
            row.Germination = PlanningLibraryDbMapper.Write(profile.Parameters.Germination);
            row.SeedlingAge = PlanningLibraryDbMapper.Write(profile.Parameters.SeedlingAge);
            row.Vegetation = PlanningLibraryDbMapper.Write(profile.Parameters.Vegetation);
            row.Extras = PlanningLibraryDbMapper.Write(profile.Extras);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(profile);
        });

    public Task<Result<SortPlanningSettings>> SaveSortSettingsAsync(SortPlanningSettings settings, CancellationToken cancellationToken)
        => WriteAsync(async () =>
        {
            await using var unit = units.Create();
            if (!await unit.Repository<PlantSort>().Rows.AnyAsync(x => x.Id == settings.SortId.Value, cancellationToken).ConfigureAwait(false))
                return Result.Failure<SortPlanningSettings>("Сорт не найден");
            var repository = unit.Repository<SortPlanningSettingsRow>();
            var row = await repository.Rows.SingleOrDefaultAsync(x => x.SortId == settings.SortId.Value, cancellationToken).ConfigureAwait(false);
            if (row is null)
            {
                row = new() { SortId = settings.SortId.Value };
                repository.Add(row);
            }
            row.Extras = PlanningLibraryDbMapper.Write(settings.Extras);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(settings);
        });

    public Task<Result<RegionalPlantingWindow>> SaveRegionalWindowAsync(RegionalPlantingWindow window, CancellationToken cancellationToken)
        => WriteAsync(async () =>
        {
            await using var unit = units.Create();
            if (!await unit.Repository<PlanningRegionRow>().Rows.AnyAsync(x => x.Id == window.RegionId.Value, cancellationToken).ConfigureAwait(false)
                || !await unit.Repository<PlantCulture>().Rows.AnyAsync(x => x.Id == window.CultureId.Value, cancellationToken).ConfigureAwait(false))
                return Result.Failure<RegionalPlantingWindow>("Регион или культура не найдены");
            var repository = unit.Repository<RegionalPlantingWindowRow>();
            var row = await repository.Rows.SingleOrDefaultAsync(x => x.RegionId == window.RegionId.Value
                && x.CultureId == window.CultureId.Value && x.Location == (int)window.Conditions.Location
                && x.Placement == (int)window.Conditions.Placement && x.Purpose == (int)window.Purpose, cancellationToken).ConfigureAwait(false);
            if (row is null)
            {
                row = new() { RegionId = window.RegionId.Value, CultureId = window.CultureId.Value,
                    Location = (int)window.Conditions.Location, Placement = (int)window.Conditions.Placement, Purpose = (int)window.Purpose };
                repository.Add(row);
            }
            row.Window = PlanningLibraryDbMapper.Write(window.Window);
            row.Source = window.Source.ToNullable();
            row.Note = window.Note.ToNullable();
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(window);
        });

    private static Task<Result<T>> WriteAsync<T>(Func<Task<Result<T>>> action)
        => DbRead.TryAsync(async () =>
        {
            try { return await action().ConfigureAwait(false); }
            catch (DbUpdateConcurrencyException) { return Result.Failure<T>("Запись справочника изменена другим процессом"); }
            catch (DbUpdateException error) { return Result.Failure<T>($"Ошибка БД: {error.InnerException?.Message ?? error.Message}"); }
        }, "справочников планирования");
}
