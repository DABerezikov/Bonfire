using System.Collections.Immutable;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Planning;

namespace Bonfire.Application.Planning;

public interface ISeedShoppingStockReader
{
    Task<Result<ImmutableArray<SeedPurchaseStock>>> ReadAsync(ImmutableArray<SeedPurchaseKey> keys, CancellationToken cancellationToken);
}

public sealed record SeedShoppingItemFromViewModel(string Culture, string Sort, SeedPurchaseDeficit Deficit, DateOnly FirstSowing);
public sealed record SeedShoppingListFromViewModel(int PlanId, int PlanVersion, string PlanName, int SeasonYear,
    DateTimeOffset CreatedAt, ImmutableArray<SeedShoppingItemFromViewModel> Items);

public interface ICreateSeedShoppingList
{
    Task<Result<SeedShoppingListFromViewModel>> ExecuteAsync(int planId, CancellationToken cancellationToken = default);
}

public sealed class CreateSeedShoppingList(IPlantingPlanReader plans, ISeedShoppingStockReader stock, TimeProvider clock) : ICreateSeedShoppingList
{
    public Task<Result<SeedShoppingListFromViewModel>> ExecuteAsync(int planId, CancellationToken cancellationToken = default)
        => SeasonalPlantingPlanId.Create(planId).Match(async id => await
            (await plans.ReadAsync(id, cancellationToken).ConfigureAwait(false)).Match(
                found => found.Match(plan => Build(plan, cancellationToken),
                    () => Task.FromResult(Result.Failure<SeedShoppingListFromViewModel>("Сезонный план не найден"))),
                error => Task.FromResult(Result.Failure<SeedShoppingListFromViewModel>(error))).ConfigureAwait(false),
            error => Task.FromResult(Result.Failure<SeedShoppingListFromViewModel>(error)));

    private async Task<Result<SeedShoppingListFromViewModel>> Build(PlantingPlanDetailsFromViewModel plan, CancellationToken cancellationToken)
    {
        var demands = plan.Batches.Traverse(batch => batch.Document.Quantity.Match(quantity => Result.Success(
                new SeedPurchaseDemand(Key(batch), quantity.Seeds.Value)),
            () => Result.Failure<SeedPurchaseDemand>($"В партии «{batch.Document.References.CultureName} / {batch.Document.References.SortName}» не задано число семян. Пересчитайте и сохраните партию.")));
        return await demands.Match(async needs =>
        {
            var inventory = await stock.ReadAsync([..needs.Select(item => item.Key).Distinct()], cancellationToken).ConfigureAwait(false);
            return inventory.Bind(available => SeedShoppingList.Calculate(needs, available).Bind(rows =>
            {
                var items = rows.Select(row =>
                {
                    var batches = plan.Batches.Where(batch => Key(batch) == row.Key).ToArray();
                    var reference = batches[0].Document.References;
                    return new SeedShoppingItemFromViewModel(reference.CultureName, reference.SortName, row,
                        batches.Min(batch => batch.Document.Batch.Choices.SowingDate.Match(date => date, () => batch.Document.Preview.Agronomy.Sowing.Start)));
                }).OrderBy(item => item.Culture).ThenBy(item => item.Sort).ToImmutableArray();
                return Result.Success(new SeedShoppingListFromViewModel(plan.Id, plan.Version, plan.Definition.Name.Value,
                    plan.Definition.SeasonYear, clock.GetUtcNow(), items));
            }));
        }, error => Task.FromResult(Result.Failure<SeedShoppingListFromViewModel>(error))).ConfigureAwait(false);
    }
    private static SeedPurchaseKey Key(StoredPlannedBatchFromViewModel batch) => new(batch.Document.Batch.CultureId, batch.Document.Batch.SortId);
}
