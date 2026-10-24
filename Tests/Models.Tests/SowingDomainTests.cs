using Bonfire.Domain.Seeds;

namespace Models.Tests;

public class SowingDomainTests
{
    [Theory]
    [InlineData(10, 2, SeedUnit.Pieces)]
    [InlineData(1.5, 0.5, SeedUnit.Grams)]
    [InlineData(2.5, 0.5, SeedUnit.Pieces)]
    [InlineData(1, 1, SeedUnit.Grams)]
    public void Sow_ValidAmount_ReturnsNewStockAndConservesQuantity(double available, double amount, SeedUnit unit)
    {
        var stock = new SeedStock(SeedAmount.Create(available, unit).ValueOrThrow());
        var requested = SeedAmount.Create(amount, unit).ValueOrThrow();

        var change = stock.Sow(requested).ValueOrThrow();

        Assert.Equal(available, stock.Available.Value);
        Assert.Equal(amount, change.Sown.Value);
        Assert.Equal(unit, change.Sown.Unit);
        Assert.Equal(unit, change.Remaining.Available.Unit);
        Assert.Equal(available, change.Sown.Value + change.Remaining.Available.Value, 10);
        Assert.NotSame(stock, change.Remaining);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Sow_ZeroOrExcess_ReturnsFailureAndPreservesStock(double amount)
    {
        var stock = new SeedStock(SeedAmount.Create(10, SeedUnit.Pieces).ValueOrThrow());
        Assert.True(stock.Sow(SeedAmount.Create(amount, SeedUnit.Pieces).ValueOrThrow()).IsFailure);
        Assert.Equal(10, stock.Available.Value);
    }

    [Fact]
    public void Sow_DifferentUnit_ReturnsFailure()
    {
        var stock = new SeedStock(SeedAmount.Create(10, SeedUnit.Pieces).ValueOrThrow());
        Assert.True(stock.Sow(SeedAmount.Create(1, SeedUnit.Grams).ValueOrThrow()).IsFailure);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Create_InvalidValue_ReturnsFailure(double value)
        => Assert.True(SeedAmount.Create(value, SeedUnit.Grams).IsFailure);

    [Fact]
    public void Create_UnknownUnit_ReturnsFailure()
        => Assert.True(SeedAmount.Create(1, (SeedUnit)42).IsFailure);

    [Fact]
    public void Domain_ReferencesOnlyRuntimeAssemblies()
    {
        var references = typeof(SeedStock).Assembly.GetReferencedAssemblies();
        Assert.All(references, reference => Assert.StartsWith("System", reference.Name));
    }
}
