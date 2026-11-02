using System.Collections.Immutable;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Bonfire.Domain.Planning;

public sealed record SeedPurchaseKey(CultureId CultureId, SortId SortId);
public sealed record SeedPurchaseDemand(SeedPurchaseKey Key, decimal Seeds);
public sealed record SeedPurchaseDeficit(SeedPurchaseKey Key, decimal RequiredSeeds, decimal KnownPieces,
    Option<decimal> KnownGrams, Option<decimal> BuySeeds, bool NeedsStockReview);

public sealed record SeedPurchaseStock
{
    private SeedPurchaseStock(SeedPurchaseKey key, decimal pieces, Option<decimal> grams, bool uncounted)
        => (Key, Pieces, Grams, HasUncountedSeeds) = (key, pieces, grams, uncounted);
    public SeedPurchaseKey Key { get; }
    public decimal Pieces { get; }
    public Option<decimal> Grams { get; }
    public bool HasUncountedSeeds { get; }

    public static Result<SeedPurchaseStock> Create(SeedPurchaseKey key, double pieces, Option<double> grams, double packWeight)
    {
        if (!double.IsFinite(packWeight) || packWeight < 0)
            return Result.Failure<SeedPurchaseStock>("Некорректный вес пакета семян");
        return Number(pieces).Bind(count => grams.Match(value => Number(value).Bind(weight => Result.Success(Option.Some(weight))),
            () => Result.Success(Option.None<decimal>())).Bind(weight => Result.Success(new SeedPurchaseStock(key, count, weight,
                count == 0 && weight.Match(value => value > 0, () => packWeight > 0)))));
    }

    private static Result<decimal> Number(double value)
    {
        if (!double.IsFinite(value) || value < 0 || value >= (double)decimal.MaxValue)
            return Result.Failure<decimal>("Остаток семян должен быть конечным неотрицательным числом в допустимом диапазоне");
        var number = (decimal)value;
        return value > 0 && number == 0 ? Result.Failure<decimal>("Остаток семян меньше поддерживаемой точности") : Result.Success(number);
    }

}

public static class SeedShoppingList
{
    public static Result<ImmutableArray<SeedPurchaseDeficit>> Calculate(IEnumerable<SeedPurchaseDemand> demands,
        IEnumerable<SeedPurchaseStock> stock)
    {
        var inventory = stock.ToLookup(item => item.Key);
        return demands.GroupBy(item => item.Key).Traverse(group =>
        {
            if (group.Any(item => item.Seeds <= 0 || decimal.Truncate(item.Seeds) != item.Seeds))
                return Result.Failure<SeedPurchaseDeficit>("Потребность в семенах должна быть положительным целым числом");
            var available = inventory[group.Key].ToArray();
            return DecimalArithmetic.Sum(group.Select(item => item.Seeds)).Bind(required =>
                DecimalArithmetic.Sum(available.Select(item => item.Pieces)).Bind(pieces =>
                    DecimalArithmetic.Sum(available.SelectMany(item => item.Grams.Match(value => new[] { value }, () => Array.Empty<decimal>())))
                    .Bind(grams => Result.Success(new SeedPurchaseDeficit(group.Key, required, pieces,
                        available.Any(item => item.Grams is Option<decimal>.Some) ? Option.Some(grams) : Option.None<decimal>(),
                        available.Any(item => item.HasUncountedSeeds) && pieces < required ? Option.None<decimal>()
                            : Option.Some(pieces >= required ? 0m : decimal.Ceiling(required - pieces)),
                        available.Any(item => item.HasUncountedSeeds) && pieces < required)))));
        }).Bind(rows => Result.Success(rows.Where(row => row.NeedsStockReview || row.BuySeeds.Match(value => value > 0, () => false)).ToImmutableArray()));
    }
}
