namespace Models.Tests;

public class SeedSortRangesTests
{
    [Theory]
    [InlineData(null, null, false)]
    [InlineData(20, null, true)]
    [InlineData(null, 30, true)]
    [InlineData(20, 30, false)]
    [InlineData(30, 20, false)]
    [InlineData(0, 0, false)]
    public void HasErrors_PlantingScheme_RequiresBothValues(int? spacing, int? rowSpacing, bool expected)
    {
        var model = new SortEditModel { Name = "Сорт", LandingPattern = spacing, LandingRowSpacing = rowSpacing };
        Assert.Equal(expected, model.HasErrors);
        Assert.Equal(expected, !string.IsNullOrEmpty(model[nameof(model.LandingPattern)]));
        Assert.Equal(expected, !string.IsNullOrEmpty(model[nameof(model.LandingRowSpacing)]));
    }

    [Theory]
    [InlineData(2.5, false)]
    [InlineData(2.55, true)]
    [InlineData(0, false)]
    public void HasErrors_Yield_AllowsOneDecimalPlace(double value, bool expected)
    {
        var model = new SortEditModel { Name = "Сорт", YieldKg = value };
        Assert.Equal(expected, model.HasErrors);
        Assert.Equal(expected, !string.IsNullOrEmpty(model[nameof(model.YieldKg)]));
    }

    [Theory]
    [InlineData(50, 40, true)]
    [InlineData(-1, 40, true)]
    [InlineData(30, 40, false)]
    [InlineData(0, 0, false)]
    public void HasErrors_SeedlingAgeRange_ValidatesBounds(int min, int max, bool expected)
    {
        var model = new SortEditModel { Name = "Сорт", AgeOfSeedlings = min, MaxAgeOfSeedlings = max };
        Assert.Equal(expected, model.HasErrors);
    }

    [Fact]
    public void HasErrors_NewNumericFields_RejectsInvalidValues()
    {
        Assert.True(new SortEditModel { Name = "Сорт", PlantHeight = 100, MaxPlantHeight = 50 }.HasErrors);
        Assert.True(new SortEditModel { Name = "Сорт", MinFruitWeight = 20, MaxFruitWeight = 10 }.HasErrors);
        Assert.True(new SortEditModel { Name = "Сорт", MinGerminationTime = 7, MaxGerminationTime = 3 }.HasErrors);
        Assert.True(new SortEditModel { Name = "Сорт", YieldKg = double.NaN }.HasErrors);
        Assert.True(new SortEditModel { Name = "Сорт", LandingRowSpacing = -1 }.HasErrors);
    }

    [Theory]
    [InlineData(90, 70, true)]
    [InlineData(70, 90, false)]
    [InlineData(70, -1, true)]
    public void HasErrors_GrowingSeasonRange_ValidatesSeparateVegetationBounds(int min, int max, bool expected)
    {
        var model = new SortEditModel { Name = "Сорт", GrowingSeason = min, MaxGrowingSeason = max,
            MinGerminationTime = 3, MaxGerminationTime = 7 };
        Assert.Equal(expected, model.HasErrors);
    }
}
