using BonfireDB.Entities;

namespace Bonfire.Models.Mappers;

/// <summary>Маппинг семян: сущность ↔ DTO и глубокое копирование сущности.</summary>
internal static class SeedMapper
{
    public static SeedsFromViewModel ToViewModel(Seed seed) => new()
    {
        Id = seed.Id,
        Culture = seed.Plant.PlantCulture.Name,
        Sort = seed.Plant.PlantSort.Name,
        Producer = seed.Plant.PlantSort.Producer.Name,
        ExpirationDate = seed.SeedsInfo.ExpirationDate,
        QuantityPack = seed.SeedsInfo.QuantityPack,
        WeightPack = seed.SeedsInfo.WeightPack,
        AmountSeedsQuantity = seed.SeedsInfo.AmountSeeds,
        AmountSeedsWeight = seed.SeedsInfo.AmountSeedsWeight
    };

    /// <summary>
    /// Глубокое копирование полей (Plant/Sort/Culture/Producer/SeedsInfo)
    /// из одной сущности в другую — используется при подготовке EditedItem.
    /// </summary>
    public static void CopyInto(Seed from, Seed to)
    {
        to.Id = from.Id;
        to.SeedsInfoId = from.SeedsInfoId;

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

        to.SeedsInfo.Id = from.SeedsInfo.Id;
        to.SeedsInfo.WeightPack = from.SeedsInfo.WeightPack;
        to.SeedsInfo.QuantityPack = from.SeedsInfo.QuantityPack;
        to.SeedsInfo.PurchaseDate = from.SeedsInfo.PurchaseDate;
        to.SeedsInfo.ExpirationDate = from.SeedsInfo.ExpirationDate;
        to.SeedsInfo.CostPack = from.SeedsInfo.CostPack;
        to.SeedsInfo.DisposeComment = from.SeedsInfo.DisposeComment;
        to.SeedsInfo.AmountSeeds = from.SeedsInfo.AmountSeeds;
        to.SeedsInfo.AmountSeedsWeight = from.SeedsInfo.AmountSeedsWeight;
        to.SeedsInfo.SeedSource = from.SeedsInfo.SeedSource;
        to.SeedsInfo.Note = from.SeedsInfo.Note;
    }
}
