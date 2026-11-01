using System.Collections.Immutable;
using System.Diagnostics;
using Bonfire.Application.Planning;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.Planning;
using Microsoft.EntityFrameworkCore;

namespace BonfireDB.Planning;

public sealed class DbPlantingPlans(IUnitOfWorkFactory units) : IPlantingPlanReader, IPlantingPlanWriter
{
    public Task<Result<ImmutableArray<PlantingPlanSummaryFromViewModel>>> ReadAllAsync(CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var rows = await unit.Repository<SeasonalPlantingPlanRow>().Rows.AsNoTracking().OrderByDescending(x => x.SeasonYear).ThenBy(x => x.Name)
                .Select(x => new PlantingPlanSummaryFromViewModel(x.Id, x.Version, x.Name, x.SeasonYear, x.RegionName))
                .ToArrayAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(rows.ToImmutableArray());
        }, "планов посадок");

    public Task<Result<Option<PlantingPlanDetailsFromViewModel>>> ReadAsync(SeasonalPlantingPlanId id, CancellationToken cancellationToken)
        => DbRead.TryAsync(async () =>
        {
            await using var unit = units.Create();
            var row = await unit.Repository<SeasonalPlantingPlanRow>().Rows.AsNoTracking().Include(x => x.Batches).AsSplitQuery()
                .SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken).ConfigureAwait(false);
            return row is null ? Result.Success(Option.None<PlantingPlanDetailsFromViewModel>())
                : Read(row).Bind(value => Result.Success(Option.Some(value)));
        }, "плана посадок");

    public Task<Result<PlantingPlanDetailsFromViewModel>> SaveAsync(PlantingPlanWrite request, CancellationToken cancellationToken)
        => WriteAsync(async () =>
        {
            ArgumentNullException.ThrowIfNull(request);
            await using var unit = units.Create();
            var repository = unit.Repository<SeasonalPlantingPlanRow>();
            var row = await request.Id.Match(id => repository.Rows.Include(x => x.Batches)
                .SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken),
                () => Task.FromResult<SeasonalPlantingPlanRow?>(new())).ConfigureAwait(false);
            if (row is null) return Result.Failure<PlantingPlanDetailsFromViewModel>("План не найден");
            if (row.Version != request.ExpectedVersion) return Result.Failure<PlantingPlanDetailsFromViewModel>("План изменён; обновите данные перед сохранением");
            if (row.Version == int.MaxValue) return Result.Failure<PlantingPlanDetailsFromViewModel>("Исчерпан диапазон версий плана");

            var header = request.Definition;
            var sameRegion = row.Id > 0 && row.SnapshotRegionId == header.RegionId.Value
                && row.RegionName == header.RegionName && row.TimeZoneId == header.TimeZoneId;
            if (!sameRegion)
            {
                var region = await unit.Repository<PlanningRegionRow>().Rows.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == header.RegionId.Value, cancellationToken).ConfigureAwait(false);
                if (region is null || region.IsArchived)
                    return Result.Failure<PlantingPlanDetailsFromViewModel>("Выберите существующий неархивный регион");
                row.RegionId = region.Id;
            }
            var original = row.Batches.ToDictionary(x => x.Id);
            var retained = new HashSet<int>();
            for (var ordinal = 0; ordinal < request.Batches.Length; ordinal++)
            {
                var item = request.Batches[ordinal];
                var existing = item.Id.Match(id => original.TryGetValue(id.Value, out var found) ? found : null, () => null);
                if (item.Id is Option<PlannedBatchId>.Some && existing is null)
                    return Result.Failure<PlantingPlanDetailsFromViewModel>("Партия удалена или принадлежит другому плану");
                var json = PlanningSnapshotJson.Write(item.Document);
                var changed = existing is null || existing.SnapshotJson != json;
                var batch = existing ?? new PlannedBatchRow { Plan = row };
                if (changed)
                {
                    var definition = item.Document.Batch;
                    if (!await unit.Repository<Plant>().Rows.AnyAsync(x => x.PlantCulture.Id == definition.CultureId.Value
                        && x.PlantSort.Id == definition.SortId.Value, cancellationToken).ConfigureAwait(false))
                        return Result.Failure<PlantingPlanDetailsFromViewModel>("Культура и сорт не связаны или удалены");
                    if (batch.Version == int.MaxValue) return Result.Failure<PlantingPlanDetailsFromViewModel>("Исчерпан диапазон версий партии");
                    batch.CultureId = definition.CultureId.Value;
                    batch.SortId = definition.SortId.Value;
                    batch.Location = (int)definition.Conditions.Location;
                    batch.Placement = (int)definition.Conditions.Placement;
                    batch.Method = MethodTag(definition.Method);
                    batch.SelectedSowingDate = definition.Choices.SowingDate.ToNullable();
                    batch.SnapshotJson = json;
                    batch.Version++;
                }
                batch.Ordinal = ordinal;
                if (existing is null) row.Batches.Add(batch);
                else retained.Add(existing.Id);
            }
            foreach (var removed in original.Values.Where(x => !retained.Contains(x.Id)))
            {
                row.Batches.Remove(removed);
                unit.Repository<PlannedBatchRow>().Remove(removed.Id);
            }
            row.Name = header.Name.Value;
            row.SeasonYear = header.SeasonYear;
            row.SnapshotRegionId = header.RegionId.Value;
            row.RegionName = header.RegionName;
            row.TimeZoneId = header.TimeZoneId;
            row.Version++;
            if (request.Id is Option<SeasonalPlantingPlanId>.None) repository.Add(row);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Read(row);
        });

    public Task<Result<int>> DeleteAsync(SeasonalPlantingPlanId id, int expectedVersion, CancellationToken cancellationToken)
        => WriteAsync(async () =>
        {
            await using var unit = units.Create();
            var repository = unit.Repository<SeasonalPlantingPlanRow>();
            var row = await repository.GetAsync(id.Value, cancellationToken).ConfigureAwait(false);
            if (row is null) return Result.Failure<int>("План не найден");
            if (expectedVersion <= 0 || row.Version != expectedVersion)
                return Result.Failure<int>("План изменён; обновите данные перед удалением");
            repository.Remove(row.Id);
            await unit.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(row.Id);
        });

    private static Result<PlantingPlanDetailsFromViewModel> Read(SeasonalPlantingPlanRow row)
    {
        var batches = ImmutableArray.CreateBuilder<StoredPlannedBatchFromViewModel>();
        foreach (var batch in row.Batches.OrderBy(x => x.Ordinal).ThenBy(x => x.Id))
        {
            var document = PlanningSnapshotJson.Read(batch.SnapshotJson);
            if (document.IsFailure) return document.Bind(_ => Result.Failure<PlantingPlanDetailsFromViewModel>("Неверный снимок"));
            var value = document.ValueOrThrow();
            if (batch.Method != MethodTag(value.Batch.Method) || batch.Location != (int)value.Batch.Conditions.Location
                || batch.Placement != (int)value.Batch.Conditions.Placement || batch.SelectedSowingDate != value.Batch.Choices.SowingDate.ToNullable())
                return Result.Failure<PlantingPlanDetailsFromViewModel>("Индексируемые поля партии не соответствуют снимку");
            batches.Add(new(batch.Id, batch.Version, batch.CultureId.ToOption(), batch.SortId.ToOption(), value));
        }
        return RegionId.Create(row.SnapshotRegionId).Bind(id => SeasonalPlanDefinition.Create(row.Name, row.SeasonYear, id, row.RegionName, row.TimeZoneId)
            .Bind(header => Result.Success(new PlantingPlanDetailsFromViewModel(row.Id, row.Version, row.RegionId.ToOption(), header, batches.ToImmutable()))));
    }

    private static string MethodTag(GrowingMethod method) => method switch
    {
        GrowingMethod.Transplant => "transplant",
        GrowingMethod.DirectSow => "direct",
        _ => throw new UnreachableException()
    };

    private static Task<Result<T>> WriteAsync<T>(Func<Task<Result<T>>> action) => DbRead.TryAsync(async () =>
    {
        try { return await action().ConfigureAwait(false); }
        catch (DbUpdateConcurrencyException) { return Result.Failure<T>("План изменён другим редактором; обновите данные"); }
        catch (DbUpdateException error) { return Result.Failure<T>($"Ошибка БД: {error.InnerException?.Message ?? error.Message}"); }
    }, "планирования");
}
