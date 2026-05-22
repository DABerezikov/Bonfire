using System;
using System.Threading.Tasks;
using Bonfire.Models;
using Bonfire.Services.Interfaces;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using BonfireDB.Entities.GardenPlanning;
using BonfireDB.Entities.GardenPlanning.SpotStates;
using MoonCalendar;

namespace Bonfire.Services;

// Бизнес-операция «посадить в ячейку» выполняется в ОДНОМ Unit of Work:
// загрузка ячейки/рассады/семени, списание инвентаря, создание записи о рассаде
// и перевод ячейки — всё в одном контексте и одним SaveChanges (атомарно).
internal class PlantingService(IUnitOfWorkFactory uowFactory, MoonPhase lunar) : IPlantingService
{
    public async Task<PlantResult> PlantAsync(PlantRequest request)
    {
        await using var uow = uowFactory.Create();

        var spot = await uow.Repository<PlantingSpot>().GetAsync(request.SpotId);
        if (spot is null) return new PlantResult(false, null);

        var newState = new PlantedSpotState();
        if (!spot.State.CanTransitionTo(newState)) return new PlantResult(false, null);

        var info = request.Kind == PlantSourceKind.Seedling
            ? await PlantFromSeedlingAsync(uow, request)
            : await PlantFromSeedAsync(uow, request);

        if (info is null) return new PlantResult(false, null);

        // Перевод ячейки: метка, дата и привязка к записи о рассаде (через навигацию,
        // чтобы FK SeedlingInfoId проставился после генерации ключа записи).
        spot.TransitionTo(newState);
        spot.Note        = request.PlantName;
        spot.PlantedDate = request.PlantedDate;
        spot.SeedlingInfo = info;

        await uow.SaveChangesAsync();
        return new PlantResult(true, info.Id);
    }

    // Высадка из готовой партии рассады: добавляем запись о высадке и списываем из остатка.
    private static async Task<SeedlingInfo?> PlantFromSeedlingAsync(IUnitOfWork uow, PlantRequest request)
    {
        var seedling = await uow.Repository<Seedling>().GetAsync(request.EntityId);
        if (seedling is null) return null;

        var available = request.IsWeightBased ? seedling.Weight : seedling.Quantity;
        if (available <= 0) return null;

        var newInfo = new SeedlingInfo
        {
            LandingDate    = request.PlantedDate,
            SeedlingSource = PlantSources.FromSeeds,
            PlantPlace     = request.PlantPlace
        };
        // Рассада отслеживается этим UoW — добавление в коллекцию проставит FK.
        seedling.SeedlingInfos.Add(newInfo);
        await uow.Repository<SeedlingInfo>().AddAsync(newInfo);

        var actualAmount = request.IsWeightBased
            ? Math.Min(request.Amount, seedling.Weight)
            : Math.Min(request.Amount, (double)seedling.Quantity);

        if (request.IsWeightBased)
            seedling.Weight = Math.Max(0, seedling.Weight - actualAmount);
        else
            seedling.Quantity = Math.Max(0, seedling.Quantity - (int)Math.Round(actualAmount));

        return newInfo;
    }

    // Прямой посев семян: создаём новую рассаду из семени и списываем из пакета.
    private async Task<SeedlingInfo?> PlantFromSeedAsync(IUnitOfWork uow, PlantRequest request)
    {
        var seed = await uow.Repository<Seed>().GetAsync(request.EntityId);
        if (seed is null) return null;

        var available = request.IsWeightBased
            ? (seed.SeedsInfo.AmountSeedsWeight ?? 0)
            : seed.SeedsInfo.AmountSeeds;
        if (available <= 0) return null;

        var actualAmount = request.IsWeightBased
            ? Math.Min(request.Amount, available)
            : Math.Min(request.Amount, (double)available);

        var moonPhase = lunar.GetMoonPhase(request.PlantedDate);
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
            newSeedling.Weight = actualAmount;
        else
            newSeedling.Quantity = (int)Math.Round(actualAmount);

        // Attach графа: новая рассада и запись → Added, существующее растение → Unchanged.
        await uow.Repository<Seedling>().AddAsync(newSeedling);

        if (request.IsWeightBased)
            seed.SeedsInfo.AmountSeedsWeight = Math.Max(0, (seed.SeedsInfo.AmountSeedsWeight ?? 0) - actualAmount);
        else
            seed.SeedsInfo.AmountSeeds = Math.Max(0, seed.SeedsInfo.AmountSeeds - (int)Math.Round(actualAmount));

        return newSeedlingInfo;
    }
}
