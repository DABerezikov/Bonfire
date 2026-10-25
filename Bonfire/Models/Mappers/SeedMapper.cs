using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using BonfireDB.Entities;

namespace Bonfire.Models.Mappers;

/// <summary>Маппинг семян: сущность ↔ DTO и глубокое копирование сущности.</summary>
internal static class SeedMapper
{
    public static bool IsPackageValid(SeedsInfo info) =>
        double.IsFinite(info.WeightPack) && info.WeightPack >= 0
        && double.IsFinite(info.QuantityPack) && info.QuantityPack >= 0
        && double.IsFinite(info.AmountSeeds) && info.AmountSeeds >= 0
        && (info.AmountSeedsWeight is null || (double.IsFinite(info.AmountSeedsWeight.Value) && info.AmountSeedsWeight >= 0))
        && info.CostPack >= 0;

    public static bool SamePackage(SeedsInfo a, SeedsInfo b) =>
        a.WeightPack == b.WeightPack && a.QuantityPack == b.QuantityPack
        && a.PurchaseDate == b.PurchaseDate && a.ExpirationDate == b.ExpirationDate
        && a.CostPack == b.CostPack && a.AmountSeeds == b.AmountSeeds
        && a.AmountSeedsWeight == b.AmountSeedsWeight && a.SeedSource == b.SeedSource
        && a.Note == b.Note && a.DisposeComment == b.DisposeComment;

    public static void CopyPackage(SeedsInfo from, SeedsInfo to)
    {
        to.WeightPack = from.WeightPack;
        to.QuantityPack = from.QuantityPack;
        to.PurchaseDate = from.PurchaseDate;
        to.ExpirationDate = from.ExpirationDate;
        to.CostPack = from.CostPack;
        to.AmountSeeds = from.AmountSeeds;
        to.AmountSeedsWeight = from.AmountSeedsWeight;
        to.SeedSource = from.SeedSource;
        to.Note = from.Note;
        to.DisposeComment = from.DisposeComment;
    }

    public static SeedsFromViewModel ToViewModel(SeedReadFromViewModel seed) => new()
    {
        Id = seed.Id,
        Culture = seed.Plant.PlantCulture.Name,
        Sort = seed.Plant.PlantSort.Name,
        Producer = seed.Plant.PlantSort.Producer.Name,
        ExpirationDate = seed.SeedsInfo.ExpirationDate,
        QuantityPack = seed.SeedsInfo.QuantityPack,
        WeightPack = seed.SeedsInfo.WeightPack,
        AmountSeedsQuantity = seed.SeedsInfo.AmountSeeds,
        AmountSeedsWeight = seed.SeedsInfo.AmountSeedsWeight.ToNullable()
    };

    public static SeedsFromViewModel ToViewModel(Seed seed)
        => ToViewModel(BonfireDB.SeedEditing.SeedReadDbMapper.Read(seed).ValueOrThrow());

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
        to.Plant.PlantSort.MaxGrowingSeason = from.Plant.PlantSort.MaxGrowingSeason;
        to.Plant.PlantSort.LandingPattern = from.Plant.PlantSort.LandingPattern;
        to.Plant.PlantSort.PlantHeight = from.Plant.PlantSort.PlantHeight;
        to.Plant.PlantSort.PlantColor = from.Plant.PlantSort.PlantColor;
        to.Plant.PlantSort.Maturity = from.Plant.PlantSort.Maturity;
        to.Plant.PlantSort.MaxAgeOfSeedlings = from.Plant.PlantSort.MaxAgeOfSeedlings;
        to.Plant.PlantSort.MaxPlantHeight = from.Plant.PlantSort.MaxPlantHeight;
        to.Plant.PlantSort.LandingRowSpacing = from.Plant.PlantSort.LandingRowSpacing;
        to.Plant.PlantSort.YieldKg = from.Plant.PlantSort.YieldKg;
        to.Plant.PlantSort.MinFruitWeight = from.Plant.PlantSort.MinFruitWeight;
        to.Plant.PlantSort.MaxFruitWeight = from.Plant.PlantSort.MaxFruitWeight;
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
