using System.Collections.Generic;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Models.Tests;

public class SeedAcquisitionTests
{
    private static SeedAcquisitionValues Values(SeedUnit unit = SeedUnit.Pieces)
        => new(" Томат ", "Сорт", "Фирма", "Овощи", "Куплено", unit, 2, 3, 12m, new(2028, 12, 31), Option.Some(""));

    public static IEnumerable<object[]> InvalidValues()
    {
        yield return [Values() with { Culture = " " }];
        yield return [Values() with { Sort = "" }];
        yield return [Values() with { Producer = " " }];
        yield return [Values() with { Class = " " }];
        yield return [Values() with { Source = "" }];
        yield return [Values() with { Unit = (SeedUnit)99 }];
        yield return [Values() with { QuantityInPack = -1 }];
        yield return [Values() with { QuantityInPack = double.NaN }];
        yield return [Values() with { PackCount = double.PositiveInfinity }];
        yield return [Values() with { PackCount = -1 }];
        yield return [Values() with { QuantityInPack = double.MaxValue, PackCount = 2 }];
        yield return [Values() with { CostPack = -1 }];
        yield return [Values() with { BestBy = default }];
    }

    [Theory]
    [MemberData(nameof(InvalidValues))]
    public void Create_InvalidInput_ReturnsFailure(SeedAcquisitionValues values)
        => Assert.True(SeedAcquisition.Create(values).IsFailure);

    [Theory]
    [InlineData(SeedUnit.Pieces)]
    [InlineData(SeedUnit.Grams)]
    public void CreatePackage_ValidAcquisition_CalculatesUnitsNormalizesNamesAndKeepsNotes(SeedUnit unit)
    {
        var acquisition = SeedAcquisition.Create(Values(unit)).ValueOrThrow();
        var time = new DateTime(2026, 10, 25, 12, 30, 0);
        var package = acquisition.CreatePackage(time).ValueOrThrow().Values;
        Assert.Equal(6, acquisition.Total.Value);
        Assert.Equal("Томат", acquisition.Values.Culture);
        Assert.Equal(time, package.PurchaseDate);
        Assert.Equal(12, package.CostPack);
        Assert.Equal("", Assert.IsType<Option<string>.Some>(package.Note).Value);
        if (unit == SeedUnit.Pieces)
        {
            Assert.Equal(2, package.QuantityPack);
            Assert.Equal(6, package.AmountSeeds);
            Assert.IsType<Option<double>.None>(package.AmountSeedsWeight);
        }
        else
        {
            Assert.Equal(2, package.WeightPack);
            Assert.Equal(6, Assert.IsType<Option<double>.Some>(package.AmountSeedsWeight).Value);
            Assert.Equal(0, package.AmountSeeds);
        }
    }

    [Theory]
    [InlineData(SeedUnit.Pieces)]
    [InlineData(SeedUnit.Grams)]
    public void Replenish_ValidPackage_ReturnsNewPackageAndPreservesMetadata(SeedUnit unit)
    {
        var original = SeedPackage.Create(new()
        {
            AmountSeeds = 10, AmountSeedsWeight = Option.Some(1d), WeightPack = .8, QuantityPack = 5,
            SeedSource = Option.Some("Подарено"), ExpirationDate = new(2028, 6, 1)
        }).ValueOrThrow();
        var acquisition = SeedAcquisition.Create(Values(unit)).ValueOrThrow();
        var time = new DateTime(2026, 10, 25);
        var updated = acquisition.Replenish(original, time).ValueOrThrow();
        Assert.NotSame(original, updated);
        Assert.Equal(10, original.Values.AmountSeeds);
        Assert.Equal(1, Assert.IsType<Option<double>.Some>(original.Values.AmountSeedsWeight).Value);
        Assert.Equal(unit == SeedUnit.Pieces ? 16 : 10, updated.Values.AmountSeeds);
        Assert.Equal(unit == SeedUnit.Grams ? 7 : 1, Assert.IsType<Option<double>.Some>(updated.Values.AmountSeedsWeight).Value);
        Assert.Equal(original.Values.ExpirationDate, updated.Values.ExpirationDate);
        Assert.Equal(original.Values.SeedSource, updated.Values.SeedSource);
        Assert.Equal(.8, updated.Values.WeightPack);
        Assert.Equal(5, updated.Values.QuantityPack);
        Assert.Equal(time, updated.Values.PurchaseDate);
    }

    [Fact]
    public void Replenish_MissingWeight_ReturnsExplicitFailure()
    {
        var acquisition = SeedAcquisition.Create(Values(SeedUnit.Grams)).ValueOrThrow();
        var original = SeedPackage.Create(new()).ValueOrThrow();
        Assert.Contains("Остаток в граммах не указан", acquisition.Replenish(original, DateTime.Today).Match(_ => "не ожидалось", error => error));
        Assert.IsType<Option<double>.None>(original.Values.AmountSeedsWeight);
    }

    [Theory]
    [InlineData(0, 3, 0)]
    [InlineData(.5, 2.5, 1.25)]
    public void CreatePackage_ZeroAndFractionalValues_PreservesExistingNumericContract(double quantity, double count, double total)
    {
        var acquisition = SeedAcquisition.Create(Values(SeedUnit.Grams) with { QuantityInPack = quantity, PackCount = count }).ValueOrThrow();
        var package = acquisition.CreatePackage(DateTime.Today).ValueOrThrow();
        Assert.Equal(total, Assert.IsType<Option<double>.Some>(package.Values.AmountSeedsWeight).Value);
    }

    [Fact]
    public void Replenish_Overflow_ReturnsFailureWithoutChangingOriginal()
    {
        var acquisition = SeedAcquisition.Create(Values() with { QuantityInPack = double.MaxValue, PackCount = 1 }).ValueOrThrow();
        var original = SeedPackage.Create(new() { AmountSeeds = double.MaxValue }).ValueOrThrow();
        Assert.True(acquisition.Replenish(original, DateTime.Today).IsFailure);
        Assert.Equal(double.MaxValue, original.Values.AmountSeeds);
    }
}
