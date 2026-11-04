using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;
using BonfireDB.Entities;

namespace BonfireDB.SeedEditing;

public static class SortCharacteristicsDbMapper
{
    public static SortCharacteristicsValues Read(PlantSort sort)
        => new()
        {
            Description = sort.Description.ToOption(),
            MinGerminationTime = sort.MinGerminationTime.ToOption(),
            MaxGerminationTime = sort.MaxGerminationTime.ToOption(),
            AgeOfSeedlings = sort.AgeOfSeedlings.ToOption(),
            MaxAgeOfSeedlings = sort.MaxAgeOfSeedlings.ToOption(),
            GrowingSeason = sort.GrowingSeason.ToOption(),
            MaxGrowingSeason = sort.MaxGrowingSeason.ToOption(),
            LandingPattern = sort.LandingPattern.ToOption(),
            LandingRowSpacing = sort.LandingRowSpacing.ToOption(),
            PlantHeight = sort.PlantHeight.ToOption(),
            MaxPlantHeight = sort.MaxPlantHeight.ToOption(),
            PlantColor = sort.PlantColor.ToOption(),
            Maturity = sort.Maturity.ToOption(),
            YieldKg = sort.YieldKg.ToOption(),
            MinFruitSize = sort.MinFruitSize.ToOption(),
            MaxFruitSize = sort.MaxFruitSize.ToOption(),
            MinFruitWeight = sort.MinFruitWeight.ToOption(),
            MaxFruitWeight = sort.MaxFruitWeight.ToOption()
        };

    public static void Apply(SortCharacteristicsValues values, PlantSort target)
    {
        target.Description = values.Description.ToNullable();
        target.MinGerminationTime = values.MinGerminationTime.ToNullable();
        target.MaxGerminationTime = values.MaxGerminationTime.ToNullable();
        target.AgeOfSeedlings = values.AgeOfSeedlings.ToNullable();
        target.MaxAgeOfSeedlings = values.MaxAgeOfSeedlings.ToNullable();
        target.GrowingSeason = values.GrowingSeason.ToNullable();
        target.MaxGrowingSeason = values.MaxGrowingSeason.ToNullable();
        target.LandingPattern = values.LandingPattern.ToNullable();
        target.LandingRowSpacing = values.LandingRowSpacing.ToNullable();
        target.PlantHeight = values.PlantHeight.ToNullable();
        target.MaxPlantHeight = values.MaxPlantHeight.ToNullable();
        target.PlantColor = values.PlantColor.ToNullable();
        target.Maturity = values.Maturity.ToNullable();
        target.YieldKg = values.YieldKg.ToNullable();
        target.MinFruitSize = values.MinFruitSize.ToNullable();
        target.MaxFruitSize = values.MaxFruitSize.ToNullable();
        target.MinFruitWeight = values.MinFruitWeight.ToNullable();
        target.MaxFruitWeight = values.MaxFruitWeight.ToNullable();
    }
}