using System;
using System.Threading.Tasks;
using Bonfire.Models;
using Bonfire.Services.Interfaces;
using BonfireDB.Entities;
using BonfireDB.Entities.GardenPlanning.SpotStates;

namespace Bonfire.Services;

internal class PlantingService(
    IGardenService gardenService,
    ISeedlingsService seedlingsService,
    ISeedsService seedsService)
    : IPlantingService
{
    // Примечание: операция состоит из нескольких сохранений (рассада/семена + ячейка),
    // каждое из которых коммитится отдельно. Истинная атомарность появится после
    // внедрения Unit of Work (см. задачу 1.1).
    public async Task<PlantResult> PlantAsync(PlantRequest request)
    {
        var spot = await gardenService.GetSpotAsync(request.SpotId);
        if (spot is null) return new PlantResult(false, null);

        var newState = new PlantedSpotState();
        if (!spot.State.CanTransitionTo(newState)) return new PlantResult(false, null);

        int? savedInfoId = request.Kind == PlantSourceKind.Seedling
            ? await PlantFromSeedlingAsync(request)
            : await PlantFromSeedAsync(request);

        if (savedInfoId is null) return new PlantResult(false, null);

        await gardenService.ChangeSpotStateAsync(
            spot, newState,
            plantLabel:     request.PlantName,
            plantedDate:    request.PlantedDate,
            seedlingInfoId: savedInfoId);

        return new PlantResult(true, savedInfoId);
    }

    // Высадка из готовой партии рассады: добавляем запись о высадке и списываем из остатка.
    private async Task<int?> PlantFromSeedlingAsync(PlantRequest request)
    {
        var seedling = await seedlingsService.GetSeedlingAsync(request.EntityId);
        if (seedling is null) return null;

        var newInfo = new SeedlingInfo
        {
            LandingDate    = request.PlantedDate,
            SeedlingSource = PlantSources.FromSeeds,
            PlantPlace     = request.PlantPlace
        };
        seedling.SeedlingInfos.Add(newInfo);
        var info = await seedlingsService.AddSeedlingInfo(newInfo);

        if (request.IsWeightBased)
            seedling.Weight = Math.Max(0, seedling.Weight - request.Amount);
        else
            seedling.Quantity = Math.Max(0, seedling.Quantity - (int)Math.Round(request.Amount));
        await seedlingsService.UpdateSeedling(seedling);

        return info.Id;
    }

    // Прямой посев семян: создаём новую рассаду из семени и списываем из пакета.
    private async Task<int?> PlantFromSeedAsync(PlantRequest request)
    {
        var seed = await seedsService.GetSeedAsync(request.EntityId);
        if (seed is null) return null;

        var moonPhase = seedlingsService.Lunar.GetMoonPhase(request.PlantedDate);
        var newSeedlingInfo = new SeedlingInfo
        {
            LandingDate    = request.PlantedDate,
            LunarPhase     = moonPhase,
            SeedlingNumber = 0,
            SeedlingSource = PlantSources.FromSeeds,
            PlantPlace     = request.PlantPlace
        };
        var newSeedling = new Seedling
        {
            Plant          = seed.Plant,
            SeedId         = seed.Id,
            LandingDate    = request.PlantedDate,
            LunarPhase     = moonPhase,
            SeedlingSource = PlantSources.FromSeeds,
            PlantPlace     = request.PlantPlace,
            SeedlingInfos  = [newSeedlingInfo]
        };
        if (request.IsWeightBased)
            newSeedling.Weight = request.Amount;
        else
            newSeedling.Quantity = (int)Math.Round(request.Amount);

        var saved = await seedlingsService.MakeASeedling(newSeedling);

        if (request.IsWeightBased)
            seed.SeedsInfo.AmountSeedsWeight = Math.Max(0, (seed.SeedsInfo.AmountSeedsWeight ?? 0) - request.Amount);
        else
            seed.SeedsInfo.AmountSeeds = Math.Max(0, seed.SeedsInfo.AmountSeeds - (int)Math.Round(request.Amount));
        await seedsService.UpdateSeed(seed);

        return saved.SeedlingInfos[0].Id;
    }
}
