using BonfireDB.Entities;
using System.Linq;

namespace Bonfire.Models.Mappers;

internal static class LibraryMapper
{
    public static SortEditModel ToEditModel(PlantSort sort)
    {
        var model = new SortEditModel
        {
            Id = sort.Id,
            Name = sort.Name,
            CultureId = sort.Plants.FirstOrDefault()?.PlantCulture.Id,
            Description = sort.Description,
            MinGerminationTime = sort.MinGerminationTime,
            MaxGerminationTime = sort.MaxGerminationTime,
            AgeOfSeedlings = sort.AgeOfSeedlings,
            GrowingSeason = sort.GrowingSeason,
            MaxGrowingSeason = sort.MaxGrowingSeason,
            LandingPattern = sort.LandingPattern,
            PlantHeight = sort.PlantHeight,
            PlantColor = sort.PlantColor,
            Maturity = sort.Maturity,
            MaxAgeOfSeedlings = sort.MaxAgeOfSeedlings,
            MaxPlantHeight = sort.MaxPlantHeight,
            LandingRowSpacing = sort.LandingRowSpacing,
            YieldKg = sort.YieldKg,
            MinFruitWeight = sort.MinFruitWeight,
            MaxFruitWeight = sort.MaxFruitWeight
        };
        model.ResetDirty();
        return model;
    }

    public static CultureEditModel ToEditModel(PlantCulture culture)
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
