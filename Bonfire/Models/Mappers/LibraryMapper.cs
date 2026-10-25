using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;
using BonfireDB.Entities;
using System.Linq;

namespace Bonfire.Models.Mappers;

internal static class LibraryMapper
{
    public static SortEditModel ToEditModel(PlantSort sort)
        => ToEditModel(sort.Id, sort.Name, (sort.Plants.FirstOrDefault()?.PlantCulture.Id).ToOption(), BonfireDB.SeedEditing.SortCharacteristicsDbMapper.Read(sort));

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

    public static CultureEditModel ToEditModel(PlantCulture culture)
        => ToEditModel(new CultureDetailsFromViewModel(culture.Id, culture.Name, culture.Class));

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

    public static void ApplyTo(SortEditModel model, PlantSort sort)
    {
        sort.Name = model.Name!;
        sort.Description = model.Description;
        sort.MinGerminationTime = model.MinGerminationTime;
        sort.MaxGerminationTime = model.MaxGerminationTime;
        sort.AgeOfSeedlings = model.AgeOfSeedlings;
        sort.GrowingSeason = model.GrowingSeason;
        sort.MaxGrowingSeason = model.MaxGrowingSeason;
        sort.LandingPattern = model.LandingPattern;
        sort.PlantHeight = model.PlantHeight;
        sort.PlantColor = model.PlantColor;
        sort.Maturity = model.Maturity;
        sort.MaxAgeOfSeedlings = model.MaxAgeOfSeedlings;
        sort.MaxPlantHeight = model.MaxPlantHeight;
        sort.LandingRowSpacing = model.LandingRowSpacing;
        sort.YieldKg = model.YieldKg;
        sort.MinFruitWeight = model.MinFruitWeight;
        sort.MaxFruitWeight = model.MaxFruitWeight;
    }

    public static void ApplyTo(CultureEditModel model, PlantCulture culture)
    {
        culture.Name = model.Name!;
        culture.Class = model.Class;
    }
}
