using System.Collections.Immutable;
using Bonfire.Application.Planning.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;

namespace Bonfire.Application.Planning;

public sealed record SavePlannedBatchFromViewModel(
    int PlanId,
    int ExpectedVersion,
    Option<int> BatchId,
    PlanningBatchInputFromViewModel Input,
    PlanningPreviewFromViewModel Preview,
    bool AcceptHistoricalSnapshot);

public interface IManagePlantingPlans
{
    Task<Result<PlantingPlanDetailsFromViewModel>> CreateAsync(string name, int year, int regionId,
        CancellationToken cancellationToken = default);

    Task<Result<PlantingPlanDetailsFromViewModel>> SaveBatchAsync(SavePlannedBatchFromViewModel request,
        CancellationToken cancellationToken = default);

    Task<Result<PlantingPlanDetailsFromViewModel>> DeleteBatchAsync(int planId, int expectedVersion, int batchId,
        CancellationToken cancellationToken = default);

    Task<Result<int>> DeletePlanAsync(int planId, int expectedVersion, CancellationToken cancellationToken = default);
}

public sealed class ManagePlantingPlans(
    IPlantingPlanReader reader,
    IPlantingPlanWriter writer,
    IPlanningContextReader contexts,
    IPlanningLibraryReader libraries) : IManagePlantingPlans
{
    public async Task<Result<PlantingPlanDetailsFromViewModel>> CreateAsync(string name, int year, int regionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var regions = await libraries.ReadRegionsAsync(false, cancellationToken).ConfigureAwait(false);
        return await regions.Match(async items =>
        {
            var region = items.FirstOrDefault(x => x.Id.Value == regionId);
            if (region is null)
                return Result.Failure<PlantingPlanDetailsFromViewModel>("Выберите существующий неархивный регион");
            return await SeasonalPlanDefinition.Create(name, year, region.Id, region.Definition.Name.Value,
                    region.Definition.TimeZoneId)
                .Bind(header => PlantingPlanWrite.Create(Option.None<SeasonalPlantingPlanId>(), 0, header, []))
                .Match(request => writer.SaveAsync(request, cancellationToken), FailureTask).ConfigureAwait(false);
        }, FailureTask).ConfigureAwait(false);
    }

    public async Task<Result<PlantingPlanDetailsFromViewModel>> SaveBatchAsync(SavePlannedBatchFromViewModel request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Input != request.Preview.Input)
            return Result.Failure<PlantingPlanDetailsFromViewModel>(
                "Ввод изменился после расчёта; выполните расчёт заново");
        if (request.Preview.MissingInputs.Length > 0 || request.Preview.Document is not
                                                         Option<PlanningBatchDocument>.Some complete
                                                     || complete.Value.Quantity is Option<SeedQuantityCalculation>.None)
            return Result.Failure<PlantingPlanDetailsFromViewModel>(
                "Заполните недостающие параметры и рассчитайте партию");
        var document = complete.Value;
        if (document.Preview.Agronomy.Diagnostics.Any(x => x.Code == PlanningDiagnosticCode.StageOrder)
            || request.Input.Choices.SowingDate.Match(
                s => request.Input.Choices.TransplantDate.Match(t => t < s, () => false), () => false))
            return Result.Failure<PlantingPlanDetailsFromViewModel>(
                "Исправьте порядок плановых этапов перед сохранением");
        var identities = RegionId.Create(request.Input.RegionId).Bind(r => CultureId.Create(request.Input.CultureId)
            .Bind(c => SortId.Create(request.Input.SortId)
                .Bind(s => Result.Success((Region: r, Culture: c, Sort: s)))));
        if (identities.IsFailure)
            return identities.Bind(_ => Result.Failure<PlantingPlanDetailsFromViewModel>("Неверные ссылки"));
        var ids = identities.ValueOrThrow();
        if (!request.AcceptHistoricalSnapshot)
        {
            var current = await contexts.ReadAsync(ids.Region, ids.Culture, ids.Sort, cancellationToken)
                .ConfigureAwait(false);
            if (current.IsFailure)
                return current.Bind(_ => Result.Failure<PlantingPlanDetailsFromViewModel>("Контекст недоступен"));
            if (current.ValueOrThrow().Fingerprint != request.Preview.ContextFingerprint ||
                current.ValueOrThrow().Region.Definition.IsArchived)
                return Result.Failure<PlantingPlanDetailsFromViewModel>(
                    "Справка изменилась; пересчитайте или явно выберите сохранение прежнего снимка");
        }

        return await (await ReadPlan(request.PlanId, cancellationToken).ConfigureAwait(false)).Match(async plan =>
        {
            if (plan.Version != request.ExpectedVersion)
                return Result.Failure<PlantingPlanDetailsFromViewModel>("План изменён; обновите данные");
            if (plan.Definition.RegionId != ids.Region)
                return Result.Failure<PlantingPlanDetailsFromViewModel>("Регион партии не совпадает с регионом плана");
            var batches = plan.Batches.Select(x =>
                    new PlannedBatchWrite(Option.Some(PlannedBatchId.Create(x.Id).ValueOrThrow()), x.Document))
                .ToImmutableArray();
            if (request.BatchId is Option<int>.Some batchId)
            {
                var checkedId = PlannedBatchId.Create(batchId.Value);
                if (checkedId.IsFailure)
                    return checkedId.Bind(_ => Result.Failure<PlantingPlanDetailsFromViewModel>("Неверный ID партии"));
                var index = -1;
                for (var i = 0; i < plan.Batches.Length; i++)
                    if (plan.Batches[i].Id == batchId.Value)
                        index = i;
                if (index < 0)
                    return Result.Failure<PlantingPlanDetailsFromViewModel>(
                        "Партия удалена или принадлежит другому плану");
                batches = batches.SetItem(index,
                    new PlannedBatchWrite(Option.Some(checkedId.ValueOrThrow()), document));
            }
            else
            {
                batches = batches.Add(new PlannedBatchWrite(Option.None<PlannedBatchId>(), document));
            }

            return await Save(plan, batches, cancellationToken).ConfigureAwait(false);
        }, FailureTask).ConfigureAwait(false);
    }

    public async Task<Result<PlantingPlanDetailsFromViewModel>> DeleteBatchAsync(int planId, int expectedVersion,
        int batchId, CancellationToken cancellationToken = default)
        => await (await ReadPlan(planId, cancellationToken).ConfigureAwait(false)).Match(async plan =>
        {
            if (plan.Version != expectedVersion)
                return Result.Failure<PlantingPlanDetailsFromViewModel>("План изменён; обновите данные");
            if (!plan.Batches.Any(x => x.Id == batchId))
                return Result.Failure<PlantingPlanDetailsFromViewModel>("Партия не найдена");
            return await Save(plan, plan.Batches.Where(x => x.Id != batchId).Select(x =>
                    new PlannedBatchWrite(Option.Some(PlannedBatchId.Create(x.Id).ValueOrThrow()), x.Document))
                .ToImmutableArray(), cancellationToken).ConfigureAwait(false);
        }, FailureTask).ConfigureAwait(false);

    public Task<Result<int>> DeletePlanAsync(int planId, int expectedVersion,
        CancellationToken cancellationToken = default)
        => SeasonalPlantingPlanId.Create(planId).Match(id => writer.DeleteAsync(id, expectedVersion, cancellationToken),
            error => Task.FromResult(Result.Failure<int>(error)));

    private Task<Result<PlantingPlanDetailsFromViewModel>> ReadPlan(int id, CancellationToken cancellationToken)
        => SeasonalPlantingPlanId.Create(id).Match(async key =>
            (await reader.ReadAsync(key, cancellationToken).ConfigureAwait(false))
            .Bind(found => found.Match(Result.Success,
                () => Result.Failure<PlantingPlanDetailsFromViewModel>("План не найден"))), FailureTask);

    private Task<Result<PlantingPlanDetailsFromViewModel>> Save(PlantingPlanDetailsFromViewModel plan,
        ImmutableArray<PlannedBatchWrite> batches, CancellationToken cancellationToken)
        => PlantingPlanWrite.Create(Option.Some(SeasonalPlantingPlanId.Create(plan.Id).ValueOrThrow()), plan.Version,
                plan.Definition, batches)
            .Match(request => writer.SaveAsync(request, cancellationToken), FailureTask);

    private static Task<Result<PlantingPlanDetailsFromViewModel>> FailureTask(string error)
        => Task.FromResult(Result.Failure<PlantingPlanDetailsFromViewModel>(error));
}