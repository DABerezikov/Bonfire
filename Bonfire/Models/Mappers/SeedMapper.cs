using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;

namespace Bonfire.Models.Mappers;

/// <summary>Маппинг независимого снимка семян в строку представления.</summary>
internal static class SeedMapper
{
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
}