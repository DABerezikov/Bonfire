using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;

namespace Bonfire.Models.Mappers;

internal static class LibraryMapper
{
    public static SortEditModel ToEditModel(SortReadFromViewModel sort)
        => ToEditModel(sort.Id, sort.Name, sort.CultureId, sort.Characteristics);

    private static SortEditModel ToEditModel(int id, string name, Option<int> cultureId, Bonfire.Domain.Seeds.SortCharacteristicsValues values)
    {
        var model = new SortEditModel
        {
            Id = id,
            Name = name,
            CultureId = cultureId.ToNullable(),
            Description = values.Description.ToNullable(),
            MinGerminationTime = values.MinGerminationTime.ToNullable(),
            MaxGerminationTime = values.MaxGerminationTime.ToNullable(),
            AgeOfSeedlings = values.AgeOfSeedlings.ToNullable(),
            GrowingSeason = values.GrowingSeason.ToNullable(),
            MaxGrowingSeason = values.MaxGrowingSeason.ToNullable(),
            LandingPattern = values.LandingPattern.ToNullable(),
            PlantHeight = values.PlantHeight.ToNullable(),
            PlantColor = values.PlantColor.ToNullable(),
            Maturity = values.Maturity.ToNullable(),
            MaxAgeOfSeedlings = values.MaxAgeOfSeedlings.ToNullable(),
            MaxPlantHeight = values.MaxPlantHeight.ToNullable(),
            LandingRowSpacing = values.LandingRowSpacing.ToNullable(),
            YieldKg = values.YieldKg.ToNullable(),
            MinFruitWeight = values.MinFruitWeight.ToNullable(),
            MaxFruitWeight = values.MaxFruitWeight.ToNullable()
        };
        model.ResetDirty();
        return model;
    }

    public static CultureEditModel ToEditModel(CultureDetailsFromViewModel culture)
    {
        var model = new CultureEditModel
        {
            Id = culture.Id,
            Name = culture.Name,
            Class = culture.Class
        };
        model.ResetDirty();
        return model;
    }

}
