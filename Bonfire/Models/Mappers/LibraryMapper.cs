using BonfireDB.Entities;

namespace Bonfire.Models.Mappers;

internal static class LibraryMapper
{
    public static SortEditModel ToEditModel(PlantSort sort)
    {
        var model = new SortEditModel
        {
            Id = sort.Id,
            Name = sort.Name,
            Description = sort.Description,
            MinGerminationTime = sort.MinGerminationTime,
            MaxGerminationTime = sort.MaxGerminationTime,
            AgeOfSeedlings = sort.AgeOfSeedlings,
            GrowingSeason = sort.GrowingSeason,
            LandingPattern = sort.LandingPattern,
            PlantHeight = sort.PlantHeight,
            PlantColor = sort.PlantColor
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
        sort.LandingPattern = model.LandingPattern;
        sort.PlantHeight = model.PlantHeight;
        sort.PlantColor = model.PlantColor;
    }

    public static void ApplyTo(CultureEditModel model, PlantCulture culture)
    {
        culture.Name = model.Name!;
        culture.Class = model.Class;
    }
}
