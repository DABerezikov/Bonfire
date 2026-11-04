using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Models.Tests;

public class SeedEditingModelsTests
{
    public static IEnumerable<object[]> InvalidPackages()
    {
        yield return [new SeedPackageValues { WeightPack = -1 }];
        yield return [new SeedPackageValues { WeightPack = double.NaN }];
        yield return [new SeedPackageValues { QuantityPack = double.PositiveInfinity }];
        yield return [new SeedPackageValues { QuantityPack = -1 }];
        yield return [new SeedPackageValues { AmountSeeds = -1 }];
        yield return [new SeedPackageValues { AmountSeeds = double.NaN }];
        yield return [new SeedPackageValues { AmountSeedsWeight = Option.Some(-1d) }];
        yield return [new SeedPackageValues { AmountSeedsWeight = Option.Some(double.NaN) }];
        yield return [new SeedPackageValues { CostPack = -1 }];
    }

    [Theory]
    [MemberData(nameof(InvalidPackages))]
    public void Package_InvalidQuantities_ReturnsFailure(SeedPackageValues values)
        => Assert.True(SeedPackage.Create(values).IsFailure);

    [Fact]
    public void Package_WithDetails_PreservesOriginalAndDistinguishesAbsentFromZero()
    {
        var original = SeedPackage.Create(new() { Note = Option.Some("") }).ValueOrThrow();
        var updated = original.WithDetails(original.Values with { AmountSeedsWeight = Option.Some(0d) }).ValueOrThrow();
        Assert.NotSame(original, updated);
        Assert.IsType<Option<double>.None>(original.Values.AmountSeedsWeight);
        Assert.Equal(0, Assert.IsType<Option<double>.Some>(updated.Values.AmountSeedsWeight).Value);
        Assert.Equal("", Assert.IsType<Option<string>.Some>(updated.Values.Note).Value);
        Assert.True(original.WithDetails(original.Values with { CostPack = -1 }).IsFailure);
    }

    public static IEnumerable<object[]> InvalidCharacteristics()
    {
        yield return [new SortCharacteristicsValues { MinGerminationTime = Option.Some(-1) }];
        yield return [new SortCharacteristicsValues { MaxGerminationTime = Option.Some(-1) }];
        yield return [new SortCharacteristicsValues { MinGerminationTime = Option.Some(5), MaxGerminationTime = Option.Some(4) }];
        yield return [new SortCharacteristicsValues { AgeOfSeedlings = Option.Some(50), MaxAgeOfSeedlings = Option.Some(40) }];
        yield return [new SortCharacteristicsValues { MaxAgeOfSeedlings = Option.Some(-1) }];
        yield return [new SortCharacteristicsValues { GrowingSeason = Option.Some(90), MaxGrowingSeason = Option.Some(80) }];
        yield return [new SortCharacteristicsValues { MaxGrowingSeason = Option.Some(-1) }];
        yield return [new SortCharacteristicsValues { PlantHeight = Option.Some(50), MaxPlantHeight = Option.Some(40) }];
        yield return [new SortCharacteristicsValues { MaxPlantHeight = Option.Some(-1) }];
        yield return [new SortCharacteristicsValues { LandingPattern = Option.Some(-1) }];
        yield return [new SortCharacteristicsValues { LandingRowSpacing = Option.Some(-1) }];
        yield return [new SortCharacteristicsValues { MinFruitWeight = Option.Some(double.NaN) }];
        yield return [new SortCharacteristicsValues { MaxFruitWeight = Option.Some(double.PositiveInfinity) }];
        yield return [new SortCharacteristicsValues { MinFruitWeight = Option.Some(2d), MaxFruitWeight = Option.Some(1d) }];
        yield return [new SortCharacteristicsValues { YieldKg = Option.Some(-1d) }];
        yield return [new SortCharacteristicsValues { YieldKg = Option.Some(double.NaN) }];
    }

    [Theory]
    [MemberData(nameof(InvalidCharacteristics))]
    public void Characteristics_InvalidRanges_ReturnsFailure(SortCharacteristicsValues values)
        => Assert.True(SortCharacteristics.Create("Сорт", values).IsFailure);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Characteristics_MissingName_ReturnsFailure(string? name)
        => Assert.True(SortCharacteristics.Create(name!, new()).IsFailure);

    [Fact]
    public void Characteristics_WithDetails_NormalizesNamesAndPreservesOptionalBoundsAndOriginal()
    {
        var original = SortCharacteristics.Create(" Сорт ", new()
            { MaxPlantHeight = Option.Some(0), PlantColor = Option.Some(" Полосатый ") }).ValueOrThrow();
        var changed = original.WithDetails("Новый", original.Values with { Description = Option.Some("") }).ValueOrThrow();
        Assert.NotSame(original, changed);
        Assert.Equal("Сорт", original.Name.Value);
        Assert.Equal("Новый", changed.Name.Value);
        Assert.IsType<Option<string>.None>(original.Values.Description);
        Assert.Equal("", Assert.IsType<Option<string>.Some>(changed.Values.Description).Value);
        Assert.Equal("Полосатый", Assert.IsType<Option<string>.Some>(changed.Values.PlantColor).Value);
        Assert.IsType<Option<int>.None>(changed.Values.PlantHeight);
        Assert.Equal(0, Assert.IsType<Option<int>.Some>(changed.Values.MaxPlantHeight).Value);
        Assert.True(original.WithDetails("", original.Values).IsFailure);
    }
}
