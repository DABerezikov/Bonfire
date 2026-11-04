using System.Collections.Immutable;
using Bonfire.Application.Harvest;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Harvest;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.Planning;
using BonfireDB.Planning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Harvest;

public sealed class DbHarvestJournal(IUnitOfWorkFactory units)
    : IHarvestJournalReader, IHarvestJournalWriter, IHarvestSourceReader
{
    public Task<Result<ImmutableArray<HarvestEntryFromViewModel>>> ReadAsync(CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var rows = await unit.Repository<HarvestEntryRow>().Rows.AsNoTracking().OrderByDescending(x => x.Date)
                .ThenBy(x => x.Id)
                .ToArrayAsync(cancellationToken).ConfigureAwait(false);
            var entries = ImmutableArray.CreateBuilder<HarvestEntryFromViewModel>();
            foreach (var row in rows)
            {
                var result = Read(row);
                if (result.IsFailure)
                    return result.Bind(_ =>
                        Result.Failure<ImmutableArray<HarvestEntryFromViewModel>>("Неверная запись журнала"));
                entries.Add(result.ValueOrThrow());
            }

            return Result.Success(entries.ToImmutable());
        }, "журнала сборов");

    public Task<Result<HarvestEntryFromViewModel>> SaveAsync(HarvestWrite request, CancellationToken cancellationToken)
        => WriteAsync(async () =>
        {
            await using var unit = units.Create();
            var repository = unit.Repository<HarvestEntryRow>();
            var row = await request.Id.Match(id => repository.GetAsync(id.Value, cancellationToken),
                () => Task.FromResult<HarvestEntryRow?>(new HarvestEntryRow())).ConfigureAwait(false);
            if (row is null || row.Version != request.ExpectedVersion || row.Version == int.MaxValue)
                return Result.Failure<HarvestEntryFromViewModel>("Сбор изменён или удалён; обновите журнал");
            var entry = request.Entry;
            if (request.Id is Option<HarvestEntryId>.None)
            {
                var identity = entry.Identity;
                if (identity.PerennialId is Option<PerennialPlantingId>.Some perennialId)
                {
                    var planting = await unit.Repository<PerennialPlantingRow>().Rows.AsNoTracking()
                        .SingleOrDefaultAsync(x => x.Id == perennialId.Value.Value, cancellationToken)
                        .ConfigureAwait(false);
                    if (planting is null)
                        return Result.Failure<HarvestEntryFromViewModel>("Межсезонная посадка удалена");
                    var read = DbPerennialPlanning.Read(planting);
                    if (read.IsFailure)
                        return read.Bind(_ => Result.Failure<HarvestEntryFromViewModel>("Неверная посадка"));
                    if (identity.SeasonYear < planting.EstablishmentYear ||
                        PerennialIdentity(planting.Id, read.ValueOrThrow().Definition, identity.SeasonYear)
                            .ValueOrThrow() != identity)
                        return Result.Failure<HarvestEntryFromViewModel>(
                            "Принадлежность посадки изменилась; обновите источники");
                    row.PerennialId = planting.Id;
                    row.CultureId = planting.CultureId;
                    row.SortId = planting.SortId;
                }
                else if (identity.BatchId is Option<PlannedBatchId>.Some batchId)
                {
                    var batch = await unit.Repository<PlannedBatchRow>().Rows.AsNoTracking().Include(x => x.Plan)
                        .SingleOrDefaultAsync(x => x.Id == batchId.Value.Value, cancellationToken)
                        .ConfigureAwait(false);
                    if (batch is null)
                        return Result.Failure<HarvestEntryFromViewModel>(
                            "Плановая партия удалена; выберите другую принадлежность сбора");
                    var document = PlanningSnapshotJson.Read(batch.SnapshotJson);
                    if (document.IsFailure)
                        return document.Bind(_ => Result.Failure<HarvestEntryFromViewModel>("Неверный снимок партии"));
                    var expected = BatchIdentity(batch, document.ValueOrThrow());
                    if (expected.IsFailure)
                        return expected.Bind(_ =>
                            Result.Failure<HarvestEntryFromViewModel>("Неверная принадлежность партии"));
                    if (identity != expected.ValueOrThrow())
                        return Result.Failure<HarvestEntryFromViewModel>("Справка партии изменилась; обновите журнал");
                    row.BatchId = batch.Id;
                    row.CultureId = batch.CultureId;
                    row.SortId = batch.SortId;
                }
                else
                {
                    var pair = await unit.Repository<Plant>().Rows.AsNoTracking()
                        .Where(x => x.PlantCulture.Id == identity.CultureId.Value &&
                                    x.PlantSort.Id == identity.SortId.Value)
                        .Select(x => new { Culture = x.PlantCulture.Name, Sort = x.PlantSort.Name })
                        .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                    if (pair is null || pair.Culture != identity.CultureName || pair.Sort != identity.SortName)
                        return Result.Failure<HarvestEntryFromViewModel>(
                            "Культура/сорт изменены; обновите варианты журнала");
                    row.CultureId = identity.CultureId.Value;
                    row.SortId = identity.SortId.Value;
                }

                row.SnapshotCultureId = identity.CultureId.Value;
                row.SnapshotSortId = identity.SortId.Value;
                row.CultureName = identity.CultureName;
                row.SortName = identity.SortName;
                row.SnapshotBatchId = identity.BatchId.Map(x => x.Value).ToNullable();
                row.SnapshotPerennialId = identity.PerennialId.Map(x => x.Value).ToNullable();
                row.SeasonYear = identity.SeasonYear;
                row.RegionName = identity.RegionName;
                row.TimeZoneId = identity.TimeZoneId;
                repository.Add(row);
            }
            else
            {
                var previous = Read(row);
                if (previous.IsFailure) return previous;
                if (previous.ValueOrThrow().Entry.Identity != entry.Identity)
                    return Result.Failure<HarvestEntryFromViewModel>("Принадлежность существующего сбора не меняется");
            }

            row.Date = entry.Date;
            row.MassKg = entry.Mass.Kg;
            row.Note = entry.Note.ToNullable();
            row.Version++;
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Read(row);
        });

    public Task<Result<int>> DeleteAsync(HarvestEntryId id, int expectedVersion, CancellationToken cancellationToken)
        => WriteAsync(async () =>
        {
            await using var unit = units.Create();
            var repository = unit.Repository<HarvestEntryRow>();
            var row = await repository.GetAsync(id.Value, cancellationToken).ConfigureAwait(false);
            if (row is null || expectedVersion <= 0 || row.Version != expectedVersion)
                return Result.Failure<int>("Сбор изменён или удалён; обновите журнал");
            repository.Remove(row.Id);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(row.Id);
        });

    public Task<Result<ImmutableArray<HarvestSourceFromViewModel>>> ReadSourcesAsync(int seasonYear,
        CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            if (seasonYear is < 1 or > 9999)
                return Result.Failure<ImmutableArray<HarvestSourceFromViewModel>>("Неверный сезон");
            await using var unit = units.Create();
            var rows = await unit.Repository<PlannedBatchRow>().Rows.AsNoTracking().Include(x => x.Plan)
                .Where(x => x.Plan.SeasonYear == seasonYear).OrderBy(x => x.Id).ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);
            var sources = ImmutableArray.CreateBuilder<HarvestSourceFromViewModel>();
            foreach (var row in rows)
            {
                var result = PlanningSnapshotJson.Read(row.SnapshotJson);
                if (result.IsFailure)
                    return result.Bind(_ =>
                        Result.Failure<ImmutableArray<HarvestSourceFromViewModel>>("Неверный снимок партии"));
                var document = result.ValueOrThrow();
                var identity = BatchIdentity(row, document);
                if (identity.IsFailure)
                    return identity.Bind(_ =>
                        Result.Failure<ImmutableArray<HarvestSourceFromViewModel>>("Неверная принадлежность"));
                var forecast = HarvestProjection.Calculate(document.Settings.Harvest, document.Batch.TargetPlants.Value)
                    .ValueOrThrow();
                sources.Add(new HarvestSourceFromViewModel(
                    $"{seasonYear}: {document.References.CultureName} / {document.References.SortName}, партия #{row.Id}",
                    identity.ValueOrThrow(), forecast.PerPlantKg, forecast.PerM2Kg));
            }

            var plantings = await unit.Repository<PerennialPlantingRow>().Rows.AsNoTracking()
                .Where(x => x.EstablishmentYear <= seasonYear)
                .OrderBy(x => x.Name).ThenBy(x => x.Id).ToArrayAsync(cancellationToken).ConfigureAwait(false);
            foreach (var planting in plantings)
            {
                var read = DbPerennialPlanning.Read(planting);
                if (read.IsFailure)
                    return read.Bind(_ =>
                        Result.Failure<ImmutableArray<HarvestSourceFromViewModel>>("Неверная многолетняя посадка"));
                var identity = PerennialIdentity(planting.Id, read.ValueOrThrow().Definition, seasonYear);
                if (identity.IsFailure)
                    return identity.Bind(_ =>
                        Result.Failure<ImmutableArray<HarvestSourceFromViewModel>>("Неверная принадлежность"));
                sources.Add(new HarvestSourceFromViewModel(
                    $"{seasonYear}: {planting.Name}, межсезонная посадка #{planting.Id}", identity.ValueOrThrow(),
                    Option.None<decimal>(), Option.None<decimal>()));
            }

            return Result.Success(sources.ToImmutable());
        }, "источников сборов");

    private static Result<HarvestIdentity> BatchIdentity(PlannedBatchRow row, PlanningBatchDocument document)
        => HarvestIdentity.Create(document.Batch.CultureId, document.Batch.SortId, document.References.CultureName,
            document.References.SortName,
            row.Plan.SeasonYear, row.Plan.RegionName, row.Plan.TimeZoneId,
            Option.Some(PlannedBatchId.Create(row.Id).ValueOrThrow()), Option.None<PerennialPlantingId>());

    private static Result<HarvestIdentity> PerennialIdentity(int id, PerennialPlantingDefinition definition,
        int seasonYear)
        => HarvestIdentity.Create(definition.CultureId, definition.SortId, definition.CultureName, definition.SortName,
            seasonYear,
            definition.RegionName, definition.TimeZoneId, Option.None<PlannedBatchId>(),
            Option.Some(PerennialPlantingId.Create(id).ValueOrThrow()));

    private static Result<Option<T>> OptionalId<T>(int? value, Func<int, Result<T>> create)
        => value.ToOption().Match(x => create(x).Bind(id => Result.Success(Option.Some(id))),
            () => Result.Success(Option.None<T>()));

    private static Result<HarvestEntryFromViewModel> Read(HarvestEntryRow row)
        => CultureId.Create(row.SnapshotCultureId).Bind(culture => SortId.Create(row.SnapshotSortId).Bind(sort =>
            OptionalId(row.SnapshotBatchId, PlannedBatchId.Create).Bind(batch =>
                OptionalId(row.SnapshotPerennialId, PerennialPlantingId.Create).Bind(perennial =>
                    HarvestIdentity.Create(culture, sort, row.CultureName, row.SortName, row.SeasonYear, row.RegionName,
                            row.TimeZoneId, batch, perennial)
                        .Bind(identity => HarvestMass.Create(row.MassKg).Bind(mass =>
                                HarvestEntry.Create(identity, row.Date, mass, row.Note.ToOption(), DateOnly.MaxValue))
                            .Bind(entry => row.Id > 0 && row.Version > 0
                                ? Result.Success(new HarvestEntryFromViewModel(row.Id, row.Version, entry,
                                    row.BatchId.ToOption(), row.CultureId.ToOption(), row.SortId.ToOption(),
                                    row.PerennialId.ToOption()))
                                : Result.Failure<HarvestEntryFromViewModel>("Неверный ID/версия сбора")))))));

    private static Task<Result<T>> WriteAsync<T>(Func<Task<Result<T>>> action) => DbRead.TryAsync(async () =>
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<T>("Сбор изменён другим редактором; обновите журнал");
        }
        catch (DbUpdateException error)
        {
            return Result.Failure<T>($"Не удалось сохранить сбор: {error.GetBaseException().Message}");
        }
    }, "сбора урожая");
}