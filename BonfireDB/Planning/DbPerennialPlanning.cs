using System.Collections.Immutable;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.Planning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planning;

public sealed class DbPerennialPlanning(IUnitOfWorkFactory units) : IPerennialPlanningReader, IPerennialPlanningWriter
{
    public Task<Result<ImmutableArray<PerennialPlantingFromViewModel>>> ReadAsync(CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var rows = await unit.Repository<PerennialPlantingRow>().Rows.AsNoTracking().OrderBy(x => x.Name)
                .ThenBy(x => x.Id).ToArrayAsync(cancellationToken).ConfigureAwait(false);
            var result = ImmutableArray.CreateBuilder<PerennialPlantingFromViewModel>();
            foreach (var row in rows)
            {
                var value = Read(row);
                if (value.IsFailure)
                    return value.Bind(_ =>
                        Result.Failure<ImmutableArray<PerennialPlantingFromViewModel>>("Неверная посадка"));
                result.Add(value.ValueOrThrow());
            }

            return Result.Success(result.ToImmutable());
        }, "межсезонных посадок");

    public Task<Result<ImmutableArray<PerennialSeasonFromViewModel>>> ReadSeasonsAsync(int planId,
        CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            if (planId <= 0) return Result.Failure<ImmutableArray<PerennialSeasonFromViewModel>>("Неверный ID плана");
            await using var unit = units.Create();
            var rows = await unit.Repository<PerennialSeasonRow>().Rows.AsNoTracking().Where(x => x.PlanId == planId)
                .OrderBy(x => x.PlantingId).ToArrayAsync(cancellationToken).ConfigureAwait(false);
            var result = ImmutableArray.CreateBuilder<PerennialSeasonFromViewModel>();
            foreach (var row in rows)
            {
                var value = Read(row);
                if (value.IsFailure)
                    return value.Bind(_ =>
                        Result.Failure<ImmutableArray<PerennialSeasonFromViewModel>>("Неверный прогноз"));
                result.Add(value.ValueOrThrow());
            }

            return Result.Success(result.ToImmutable());
        }, "сезонных периодов");

    public Task<Result<PerennialPlantingFromViewModel>> SaveAsync(PerennialWrite request,
        CancellationToken cancellationToken)
        => WriteAsync(async () =>
        {
            await using var unit = units.Create();
            var repository = unit.Repository<PerennialPlantingRow>();
            var row = await request.Id.Match(id => repository.GetAsync(id.Value, cancellationToken),
                () => Task.FromResult<PerennialPlantingRow?>(new PerennialPlantingRow())).ConfigureAwait(false);
            if (row is null || row.Version != request.ExpectedVersion || row.Version == int.MaxValue)
                return Result.Failure<PerennialPlantingFromViewModel>("Посадка изменена или удалена; обновите данные");
            var definition = request.Definition;
            PerennialPlantingDefinition? previous = null;
            if (row.Id > 0)
            {
                var stored = Read(row);
                if (stored.IsFailure) return stored;
                previous = stored.ValueOrThrow().Definition;
            }

            if (previous is null || definition.CultureId != previous.CultureId ||
                definition.SortId != previous.SortId ||
                definition.CultureName != previous.CultureName || definition.SortName != previous.SortName)
            {
                var pair = await unit.Repository<Plant>().Rows.AsNoTracking().Where(x =>
                        x.PlantCulture.Id == definition.CultureId.Value && x.PlantSort.Id == definition.SortId.Value)
                    .Select(x => new { Culture = x.PlantCulture.Name, Sort = x.PlantSort.Name })
                    .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                if (pair is null || pair.Culture != definition.CultureName || pair.Sort != definition.SortName)
                    return Result.Failure<PerennialPlantingFromViewModel>("Культура/сорт изменены; обновите варианты");
                row.CultureId = definition.CultureId.Value;
                row.SortId = definition.SortId.Value;
            }

            if (previous is null || definition.RegionId != previous.RegionId ||
                definition.RegionName != previous.RegionName || definition.TimeZoneId != previous.TimeZoneId)
            {
                var region = await unit.Repository<PlanningRegionRow>().Rows.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == definition.RegionId.Value, cancellationToken)
                    .ConfigureAwait(false);
                if (region is null || region.IsArchived || region.Name != definition.RegionName ||
                    region.TimeZoneId != definition.TimeZoneId)
                    return Result.Failure<PerennialPlantingFromViewModel>("Регион изменён; обновите варианты");
                row.RegionId = region.Id;
            }

            row.Name = definition.Name;
            row.EstablishmentYear = definition.EstablishmentYear;
            row.DefinitionJson = PlanningSnapshotJson.WritePerennial(definition);
            row.Version++;
            if (request.Id is Option<PerennialPlantingId>.None) repository.Add(row);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Read(row);
        });

    public Task<Result<int>> DeleteAsync(PerennialPlantingId id, int expectedVersion,
        CancellationToken cancellationToken)
        => WriteAsync(async () =>
        {
            await using var unit = units.Create();
            var repository = unit.Repository<PerennialPlantingRow>();
            var row = await repository.GetAsync(id.Value, cancellationToken).ConfigureAwait(false);
            if (row is null || expectedVersion <= 0 || row.Version != expectedVersion)
                return Result.Failure<int>("Посадка изменена или удалена");
            repository.Remove(row.Id);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(row.Id);
        });

    public Task<Result<SavedPerennialSeason>> SaveSeasonAsync(PerennialSeasonPreview preview,
        CancellationToken cancellationToken)
        => WriteAsync(async () =>
        {
            var valid = preview.Snapshot.Validate();
            if (valid.IsFailure) return valid.Bind(_ => Result.Failure<SavedPerennialSeason>("Неверный снимок"));
            await using var unit = units.Create();
            var plan = await unit.Repository<SeasonalPlantingPlanRow>().GetAsync(preview.PlanId, cancellationToken)
                .ConfigureAwait(false);
            var planting = await unit.Repository<PerennialPlantingRow>().GetAsync(preview.PlantingId, cancellationToken)
                .ConfigureAwait(false);
            if (plan is null || planting is null || preview.PlanVersion <= 0 || plan.Version != preview.PlanVersion ||
                planting.Version != preview.PlantingVersion)
                return Result.Failure<SavedPerennialSeason>("План/посадка изменены; обновите и пересчитайте прогноз");
            var fresh = Read(planting);
            if (fresh.IsFailure) return fresh.Bind(_ => Result.Failure<SavedPerennialSeason>("Неверная посадка"));
            if (fresh.ValueOrThrow().Definition != preview.Snapshot.Definition ||
                plan.SeasonYear != preview.Snapshot.Projection.SeasonYear ||
                plan.SnapshotRegionId != preview.Snapshot.Definition.RegionId.Value)
                return Result.Failure<SavedPerennialSeason>(
                    "Снимок не соответствует посадке, региону или сезону плана");
            var repository = unit.Repository<PerennialSeasonRow>();
            var row = await repository.Rows
                .SingleOrDefaultAsync(x => x.PlanId == plan.Id && x.PlantingId == planting.Id, cancellationToken)
                .ConfigureAwait(false);
            if (plan.Version == int.MaxValue || row?.Version == int.MaxValue)
                return Result.Failure<SavedPerennialSeason>("Исчерпан диапазон версий");
            if (row is null)
            {
                row = new PerennialSeasonRow { PlanId = plan.Id, PlantingId = planting.Id };
                repository.Add(row);
            }

            row.SnapshotJson = PlanningSnapshotJson.WritePerennialSeason(preview.Snapshot);
            row.SeasonYear = plan.SeasonYear;
            row.Version++;
            plan.Version++;
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Read(row).Bind(season => Result.Success(new SavedPerennialSeason(season, plan.Version)));
        });

    public Task<Result<int>> DeleteSeasonAsync(int planId, int expectedPlanVersion, PerennialPlantingId plantingId,
        CancellationToken cancellationToken)
        => WriteAsync(async () =>
        {
            await using var unit = units.Create();
            var plan = await unit.Repository<SeasonalPlantingPlanRow>().GetAsync(planId, cancellationToken)
                .ConfigureAwait(false);
            if (plan is null || expectedPlanVersion <= 0 || plan.Version != expectedPlanVersion ||
                plan.Version == int.MaxValue) return Result.Failure<int>("План изменён; обновите данные");
            var repository = unit.Repository<PerennialSeasonRow>();
            var row = await repository.Rows
                .SingleOrDefaultAsync(x => x.PlanId == planId && x.PlantingId == plantingId.Value, cancellationToken)
                .ConfigureAwait(false);
            if (row is null) return Result.Failure<int>("Сезонное представление не найдено");
            repository.Remove(row.Id);
            plan.Version++;
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(plan.Version);
        });

    internal static Result<PerennialPlantingFromViewModel> Read(PerennialPlantingRow row) => PlanningSnapshotJson
        .ReadPerennial(row.DefinitionJson).Bind(definition =>
            row.Id > 0 && row.Version > 0 && row.Name == definition.Name &&
            row.EstablishmentYear == definition.EstablishmentYear
                ? Result.Success(new PerennialPlantingFromViewModel(row.Id, row.Version, definition,
                    row.CultureId.ToOption(), row.SortId.ToOption(), row.RegionId.ToOption()))
                : Result.Failure<PerennialPlantingFromViewModel>("Индексируемые поля посадки не соответствуют снимку"));

    private static Result<PerennialSeasonFromViewModel> Read(PerennialSeasonRow row) => PlanningSnapshotJson
        .ReadPerennialSeason(row.SnapshotJson).Bind(snapshot =>
            row.Id > 0 && row.Version > 0 && row.SeasonYear == snapshot.Projection.SeasonYear
                ? Result.Success(new PerennialSeasonFromViewModel(row.Id, row.Version, row.PlanId, row.PlantingId,
                    snapshot))
                : Result.Failure<PerennialSeasonFromViewModel>("Неверные индексируемые поля сезонного прогноза"));

    private static Task<Result<T>> WriteAsync<T>(Func<Task<Result<T>>> action) => DbRead.TryAsync(async () =>
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<T>("Посадка/план изменены другим редактором");
        }
        catch (DbUpdateException error)
        {
            return Result.Failure<T>($"Не удалось сохранить: {error.GetBaseException().Message}");
        }
    }, "межсезонной посадки");
}