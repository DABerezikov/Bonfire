using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;

namespace Models.Tests;

public class SeedShoppingListTests
{
    private static SeedPurchaseKey Key => new(CultureId.Create(1).ValueOrThrow(), SortId.Create(2).ValueOrThrow());
    [Fact]
    public void AllBatchesShareStockOnce_AndDeficitIsRoundedToWholeSeeds()
    {
        var result = SeedShoppingList.Calculate([new(Key, 10), new(Key, 15)],
            [SeedPurchaseStock.Create(Key, 7.5, Option.None<double>(), 0).ValueOrThrow()]).ValueOrThrow();
        var row = Assert.Single(result);
        Assert.Equal(25m, row.RequiredSeeds); Assert.Equal(7.5m, row.KnownPieces); Assert.Equal(Option.Some(18m), row.BuySeeds);
    }
    [Fact]
    public void MissingStock_BuysWholeDemand_AndCoveredDemandIsOmitted()
    {
        Assert.Equal(Option.Some(5m), Assert.Single(SeedShoppingList.Calculate([new(Key, 5)], []).ValueOrThrow()).BuySeeds);
        Assert.Empty(SeedShoppingList.Calculate([new(Key, 5)], [SeedPurchaseStock.Create(Key, 8, Option.None<double>(), 0).ValueOrThrow()]).ValueOrThrow());
    }
    [Fact]
    public void GramStock_IsNeverInventedAsPieceCount()
    {
        var row = Assert.Single(SeedShoppingList.Calculate([new(Key, 5)],
            [SeedPurchaseStock.Create(Key, 0, Option.Some(.2), 1).ValueOrThrow()]).ValueOrThrow());
        Assert.True(row.NeedsStockReview); Assert.Equal(Option.None<decimal>(), row.BuySeeds); Assert.Equal(Option.Some(.2m), row.KnownGrams);
    }
    [Fact]
    public void InvalidStockAndOverflowAreFailures()
    {
        Assert.True(SeedPurchaseStock.Create(Key, double.PositiveInfinity, Option.None<double>(), 0).IsFailure);
        Assert.True(SeedPurchaseStock.Create(Key, -1, Option.None<double>(), 0).IsFailure);
        Assert.True(SeedShoppingList.Calculate([new(Key, decimal.MaxValue), new(Key, 1)], []).IsFailure);
    }
}
