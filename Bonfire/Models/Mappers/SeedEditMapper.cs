using System;
using Bonfire.Application.Seeds;
using Bonfire.Domain.Functional;

namespace Bonfire.Models.Mappers;

internal static class SeedEditMapper
{
    public static SeedEditorDraft FromSeed(SeedReadFromViewModel seed)
        => new(seed.Id, seed.Plant.PlantCulture.Name, seed.Plant.PlantSort.Name, seed.Plant.PlantSort.Producer.Id, seed.SeedsInfo);

    public static Result<EditSeedFromViewModel> ToRequest(SeedEditorDraft draft, SortEditModel sort, int producerId)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(sort);
        if (sort.Name is not { } name)
            return Result.Failure<EditSeedFromViewModel>("Название не может быть пустым");
        return Result.Success(new EditSeedFromViewModel(draft.Id, sort.Id, producerId, name,
            draft.Package.Values,
            new()
            {
                Description = sort.Description.ToOption(),
                MinGerminationTime = sort.MinGerminationTime.ToOption(),
                MaxGerminationTime = sort.MaxGerminationTime.ToOption(),
                AgeOfSeedlings = sort.AgeOfSeedlings.ToOption(),
                MaxAgeOfSeedlings = sort.MaxAgeOfSeedlings.ToOption(),
                GrowingSeason = sort.GrowingSeason.ToOption(),
                MaxGrowingSeason = sort.MaxGrowingSeason.ToOption(),
                LandingPattern = sort.LandingPattern.ToOption(),
                LandingRowSpacing = sort.LandingRowSpacing.ToOption(),
                PlantHeight = sort.PlantHeight.ToOption(),
                MaxPlantHeight = sort.MaxPlantHeight.ToOption(),
                PlantColor = sort.PlantColor.ToOption(),
                Maturity = sort.Maturity.ToOption(),
                YieldKg = sort.YieldKg.ToOption(),
                MinFruitSize = sort.MinFruitSize.ToOption(),
                MaxFruitSize = sort.MaxFruitSize.ToOption(),
                MinFruitWeight = sort.MinFruitWeight.ToOption(),
                MaxFruitWeight = sort.MaxFruitWeight.ToOption()
            }));
    }
}
