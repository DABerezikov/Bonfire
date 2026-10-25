using System;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;
using BonfireDB.Entities;

namespace Bonfire.Models.Mappers;

internal static class SeedEditMapper
{
    public static Result<EditSeedFromViewModel> ToRequest(Seed seed, SortEditModel sort, int producerId)
    {
        ArgumentNullException.ThrowIfNull(seed);
        ArgumentNullException.ThrowIfNull(sort);
        if (sort.Name is not { } name)
            return Result.Failure<EditSeedFromViewModel>("Название не может быть пустым");
        return Result.Success(new EditSeedFromViewModel(seed.Id, sort.Id, producerId, name,
            new()
            {
                WeightPack = seed.SeedsInfo.WeightPack,
                QuantityPack = seed.SeedsInfo.QuantityPack,
                AmountSeeds = seed.SeedsInfo.AmountSeeds,
                AmountSeedsWeight = ToOption(seed.SeedsInfo.AmountSeedsWeight),
                CostPack = seed.SeedsInfo.CostPack,
                PurchaseDate = seed.SeedsInfo.PurchaseDate,
                ExpirationDate = seed.SeedsInfo.ExpirationDate,
                Note = ToOption(seed.SeedsInfo.Note),
                SeedSource = ToOption(seed.SeedsInfo.SeedSource),
                DisposeComment = ToOption(seed.SeedsInfo.DisposeComment)
            },
            new()
            {
                Description = ToOption(sort.Description),
                MinGerminationTime = ToOption(sort.MinGerminationTime),
                MaxGerminationTime = ToOption(sort.MaxGerminationTime),
                AgeOfSeedlings = ToOption(sort.AgeOfSeedlings),
                MaxAgeOfSeedlings = ToOption(sort.MaxAgeOfSeedlings),
                GrowingSeason = ToOption(sort.GrowingSeason),
                MaxGrowingSeason = ToOption(sort.MaxGrowingSeason),
                LandingPattern = ToOption(sort.LandingPattern),
                LandingRowSpacing = ToOption(sort.LandingRowSpacing),
                PlantHeight = ToOption(sort.PlantHeight),
                MaxPlantHeight = ToOption(sort.MaxPlantHeight),
                PlantColor = ToOption(sort.PlantColor),
                Maturity = ToOption(sort.Maturity),
                YieldKg = ToOption(sort.YieldKg),
                MinFruitWeight = ToOption(sort.MinFruitWeight),
                MaxFruitWeight = ToOption(sort.MaxFruitWeight)
            }));
    }

    private static Option<T> ToOption<T>(T? value) where T : struct
        => value is { } present ? Option.Some(present) : Option.None<T>();
    private static Option<string> ToOption(string? value)
        => value is { } present ? Option.Some(present) : Option.None<string>();
}
