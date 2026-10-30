using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Models.Tests;

public class SeedReturnTests
{
    [Theory]
    [InlineData(8, 2, SeedUnit.Pieces)]
    [InlineData(1, 0.5, SeedUnit.Grams)]
    [InlineData(2, 0.5, SeedUnit.Pieces)]
    [InlineData(0, 3, SeedUnit.Pieces)]
    [InlineData(10, 0, SeedUnit.Grams)]
    public void WithReturned_ValidAmount_AddsQuantityWithoutChangingOriginal(double available, double returned, SeedUnit unit)
    {
        var stock = new SeedStock(SeedAmount.Create(available, unit).ValueOrThrow());
        var amount = SeedAmount.Create(returned, unit).ValueOrThrow();

        var result = stock.WithReturned(amount).ValueOrThrow();

        Assert.Equal(available + returned, result.Available.Value);
        Assert.Equal(unit, result.Available.Unit);
        Assert.Equal(available, stock.Available.Value);
        Assert.Equal(returned, amount.Value);
        Assert.NotSame(stock, result);
    }

    [Theory]
    [InlineData(SeedUnit.Pieces, SeedUnit.Grams)]
    [InlineData(SeedUnit.Grams, SeedUnit.Pieces)]
    public void WithReturned_DifferentUnit_ReturnsFailureAndPreservesStock(SeedUnit unit, SeedUnit other)
    {
        var stock = new SeedStock(SeedAmount.Create(10, unit).ValueOrThrow());
        Assert.True(stock.WithReturned(SeedAmount.Create(1, other).ValueOrThrow()).IsFailure);
        Assert.Equal(10, stock.Available.Value);
    }

    [Fact]
    public void WithReturned_Overflow_ReturnsFailureAndPreservesStock()
    {
        var amount = SeedAmount.Create(double.MaxValue, SeedUnit.Grams).ValueOrThrow();
        var stock = new SeedStock(amount);
        Assert.True(stock.WithReturned(amount).IsFailure);
        Assert.Equal(double.MaxValue, stock.Available.Value);
    }

    [Theory]
    [InlineData(SeedUnit.Pieces)]
    [InlineData(SeedUnit.Grams)]
    public void WithReturned_AfterSowing_RestoresOriginalStock(SeedUnit unit)
    {
        var stock = new SeedStock(SeedAmount.Create(10, unit).ValueOrThrow());
        var change = stock.Sow(SeedAmount.Create(2, unit).ValueOrThrow()).ValueOrThrow();
        var restored = change.Remaining.WithReturned(change.Sown).ValueOrThrow();
        Assert.Equal(stock.Available, restored.Available);
        Assert.Equal(8, change.Remaining.Available.Value);
    }

    private static SeedPackageValues Values() => new()
    {
        WeightPack = 2, QuantityPack = 20, AmountSeeds = 10, AmountSeedsWeight = Option.Some(1.5),
        CostPack = 150, PurchaseDate = new(2026, 1, 1), ExpirationDate = new(2028, 12, 31),
        Note = Option.Some(" Заметка "), SeedSource = Option.Some("Куплено"), DisposeComment = Option.Some("")
    };

    [Theory]
    [InlineData(SeedUnit.Pieces)]
    [InlineData(SeedUnit.Grams)]
    public void PackageWithReturned_ValidAmount_ChangesOnlyChosenStock(SeedUnit unit)
    {
        var values = Values();
        var package = SeedPackage.Create(values).ValueOrThrow();
        var result = package.WithReturned(SeedAmount.Create(0.5, unit).ValueOrThrow()).ValueOrThrow();
        var expected = unit == SeedUnit.Pieces
            ? values with { AmountSeeds = 10.5 }
            : values with { AmountSeedsWeight = Option.Some(2.0) };
        Assert.Equal(expected, result.Values);
        Assert.Equal(values, package.Values);
        Assert.NotSame(package, result);
    }

    [Theory]
    [InlineData(0, SeedUnit.Grams)]
    [InlineData(2, SeedUnit.Pieces)]
    public void PackageWithReturned_MissingWeightAndNoWeightReturned_PreservesAbsence(double quantity, SeedUnit unit)
    {
        var values = Values() with { AmountSeedsWeight = Option.None<double>() };
        var package = SeedPackage.Create(values).ValueOrThrow();
        var result = package.WithReturned(SeedAmount.Create(quantity, unit).ValueOrThrow()).ValueOrThrow();
        Assert.Equal(values with { AmountSeeds = 10 + quantity }, result.Values);
        Assert.NotSame(package, result);
    }

    [Fact]
    public void PackageWithReturned_MissingWeightAndPositiveGrams_ReturnsFailure()
    {
        var values = Values() with { AmountSeedsWeight = Option.None<double>() };
        var package = SeedPackage.Create(values).ValueOrThrow();
        Assert.True(package.WithReturned(SeedAmount.Create(1, SeedUnit.Grams).ValueOrThrow()).IsFailure);
        Assert.Equal(values, package.Values);
    }

    [Theory]
    [InlineData(SeedUnit.Pieces)]
    [InlineData(SeedUnit.Grams)]
    public void PackageWithReturned_Overflow_ReturnsFailure(SeedUnit unit)
    {
        var values = Values() with { AmountSeeds = double.MaxValue, AmountSeedsWeight = Option.Some(double.MaxValue) };
        var package = SeedPackage.Create(values).ValueOrThrow();
        Assert.True(package.WithReturned(SeedAmount.Create(double.MaxValue, unit).ValueOrThrow()).IsFailure);
        Assert.Equal(values, package.Values);
    }

    [Fact]
    public void WithReturned_NullAmount_RejectsBrokenInvariant()
    {
        var stock = new SeedStock(SeedAmount.Create(1, SeedUnit.Pieces).ValueOrThrow());
        var package = SeedPackage.Create(Values()).ValueOrThrow();
        Assert.Throws<ArgumentNullException>(() => stock.WithReturned(null!));
        Assert.Throws<ArgumentNullException>(() => package.WithReturned(null!));
    }
}
