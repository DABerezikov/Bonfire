using BonfireDB.Entities;
using System.Linq;

namespace Bonfire.Models.Mappers;

/// <summary>Глубокое копирование рассады из одной сущности в другую.</summary>
internal static class SeedlingMapper
{
    /// <summary>
    /// Копирует поля (вес/количество, Plant/Sort/Culture/Producer и список SeedlingInfos)
    /// из одной сущности в другую — используется при подготовке EditedItem.
    /// </summary>
    public static void CopyInto(Seedling from, Seedling to)
    {
        to.Id = from.Id;
        to.Weight = from.Weight;
        to.Quantity = from.Quantity;
        to.SeedId = from.SeedId;
        to.PlantedOut = from.PlantedOut;
        to.IsPlantedInBed = from.IsPlantedInBed;
        to.LandingDate = from.LandingDate;
        to.LunarPhase = from.LunarPhase;
        to.SeedlingSource = from.SeedlingSource;
        to.PlantPlace = from.PlantPlace;

        to.Plant.Id = from.Plant.Id;
        to.Plant.PlantCulture.Id = from.Plant.PlantCulture.Id;
        to.Plant.PlantCulture.Name = from.Plant.PlantCulture.Name;
        to.Plant.PlantCulture.Class = from.Plant.PlantCulture.Class;
        to.Plant.PlantSort.Id = from.Plant.PlantSort.Id;
        to.Plant.PlantSort.Name = from.Plant.PlantSort.Name;
        to.Plant.PlantSort.Description = from.Plant.PlantSort.Description;
        to.Plant.PlantSort.MinGerminationTime = from.Plant.PlantSort.MinGerminationTime;
        to.Plant.PlantSort.MaxGerminationTime = from.Plant.PlantSort.MaxGerminationTime;
        to.Plant.PlantSort.AgeOfSeedlings = from.Plant.PlantSort.AgeOfSeedlings;
        to.Plant.PlantSort.GrowingSeason = from.Plant.PlantSort.GrowingSeason;
        to.Plant.PlantSort.MaxGrowingSeason = from.Plant.PlantSort.MaxGrowingSeason;
        to.Plant.PlantSort.Maturity = from.Plant.PlantSort.Maturity;
        to.Plant.PlantSort.MaxPlantHeight = from.Plant.PlantSort.MaxPlantHeight;
        to.Plant.PlantSort.MaxAgeOfSeedlings = from.Plant.PlantSort.MaxAgeOfSeedlings;
        to.Plant.PlantSort.LandingRowSpacing = from.Plant.PlantSort.LandingRowSpacing;
        to.Plant.PlantSort.MinFruitWeight = from.Plant.PlantSort.MinFruitWeight;
        to.Plant.PlantSort.MaxFruitWeight = from.Plant.PlantSort.MaxFruitWeight;
        to.Plant.PlantSort.YieldKg = from.Plant.PlantSort.YieldKg;
        to.Plant.PlantSort.LandingPattern = from.Plant.PlantSort.LandingPattern;
        to.Plant.PlantSort.PlantHeight = from.Plant.PlantSort.PlantHeight;
        to.Plant.PlantSort.PlantColor = from.Plant.PlantSort.PlantColor;
        to.Plant.PlantSort.Producer.Id = from.Plant.PlantSort.Producer.Id;
        to.Plant.PlantSort.Producer.Name = from.Plant.PlantSort.Producer.Name;

        to.SeedlingInfos = from.SeedlingInfos.Select(CopyInfo).ToList();
    }

    private static SeedlingInfo CopyInfo(SeedlingInfo source)
    {
        var copy = new SeedlingInfo
        {
            Id = source.Id, SeedlingNumber = source.SeedlingNumber,
            MotherPlantId = source.MotherPlantId, LandingDate = source.LandingDate,
            LunarPhase = source.LunarPhase, PlantPlace = source.PlantPlace,
            GerminationDate = source.GerminationDate,
            QuarantineStartDate = source.QuarantineStartDate,
            QuarantineStopDate = source.QuarantineStopDate,
            QuarantineCause = source.QuarantineCause, QuarantineNote = source.QuarantineNote,
            SeedlingSource = source.SeedlingSource, Note = source.Note,
            QuenchingDate = source.QuenchingDate, DeathNote = source.DeathNote,
            IsDead = source.IsDead
        };
        copy.Replants = source.Replants?.Select(item => new Replanting
        {
            Id = item.Id, ReplantingDate = item.ReplantingDate,
            PotVolume = item.PotVolume, ReplantingNote = item.ReplantingNote,
            SeedlingInfo = copy
        }).ToList();
        copy.Treatments = source.Treatments?.Select(item => new Treatment
        {
            Id = item.Id, TreatmentDate = item.TreatmentDate,
            Product = item.Product, TreatmentMethod = item.TreatmentMethod,
            SeedlingInfo = copy
        }).ToList();
        return copy;
    }
}
