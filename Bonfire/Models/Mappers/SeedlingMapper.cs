using BonfireDB.Entities;

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
        to.Plant.PlantSort.LandingPattern = from.Plant.PlantSort.LandingPattern;
        to.Plant.PlantSort.PlantHeight = from.Plant.PlantSort.PlantHeight;
        to.Plant.PlantSort.PlantColor = from.Plant.PlantSort.PlantColor;
        to.Plant.PlantSort.Producer.Id = from.Plant.PlantSort.Producer.Id;
        to.Plant.PlantSort.Producer.Name = from.Plant.PlantSort.Producer.Name;

        to.SeedlingInfos = from.SeedlingInfos;
    }
}
