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

    // Высадка взошедшей рассады: списываем из доступного у источника и создаём
    // отдельную «высаженную партию» (новую Seedling в грядке) — отдельной строкой.
    // Из рассады сажают ВЗОШЕДШИЕ живые ростки штучно (вес рассады не при чём).
    // Доступно = взошедшие живые − уже высаженные; нельзя высадить больше доступного.
    private async Task<SeedlingInfo?> PlantFromSeedlingAsync(IUnitOfWork uow, PlantRequest request)
    {
        var source = await uow.Repository<Seedling>().GetAsync(request.EntityId);
        if (source is null) return null;
        if (source.IsPlantedInBed) return null; // уже в грядке — повторно не сажаем (только пересадка)

        var available = SeedlingAvailability.Available(source);
        if (available <= 0) return null; // взошедшей живой рассады для высадки нет

        var amount = Math.Min(request.Amount, available);

        // Источнику засчитываем высаженные ростки (Quantity/Weight/посеяно не трогаем).
        source.PlantedOut += (int)Math.Round(amount);

        // Высаженная партия — штучная (ростки), даже если рассада посеяна по весу.
        return CreatePlantedBatch(uow, source.Plant, source.SeedId, amount, weightBased: false,
            request.PlantedDate, request.PlantPlace);
    }

    // Прямой посев семян: списываем из пакета и создаём новую рассаду в грядке.
    // Нельзя посеять больше, чем есть семян.
    private async Task<SeedlingInfo?> PlantFromSeedAsync(IUnitOfWork uow, PlantRequest request)
    {
        var seed = await uow.Repository<Seed>().GetAsync(request.EntityId);
        if (seed is null) return null;

        var available = request.IsWeightBased ? (seed.SeedsInfo.AmountSeedsWeight ?? 0) : seed.SeedsInfo.AmountSeeds;
        if (available <= 0) return null; // семян нет

        var amount = Math.Min(request.Amount, available);

        if (request.IsWeightBased)
            seed.SeedsInfo.AmountSeedsWeight = Math.Max(0, (seed.SeedsInfo.AmountSeedsWeight ?? 0) - amount);
        else
            seed.SeedsInfo.AmountSeeds = Math.Max(0, seed.SeedsInfo.AmountSeeds - (int)Math.Round(amount));

        return CreatePlantedBatch(uow, seed.Plant, seed.Id, amount, request.IsWeightBased,
            request.PlantedDate, request.PlantPlace);
    }

    // Создаёт отдельную «высаженную партию» в грядке: новую Seedling с количеством =
    // высажено и привязкой к месту. plant/seedId берутся у источника (рассада или семя).
    // Attach графа: новая партия и её запись → Added, существующее растение → Unchanged.
    private SeedlingInfo CreatePlantedBatch(IUnitOfWork uow, Plant plant, int seedId, double amount,
        bool weightBased, DateTime plantedDate, string? plantPlace)
    {
        var moonPhase = lunar.GetMoonPhase(plantedDate);
        var info = new SeedlingInfo
        {
            LandingDate    = plantedDate,
            LunarPhase     = moonPhase,
            SeedlingNumber = 0,
            SeedlingSource = PlantSources.FromSeeds,
            PlantPlace     = plantPlace
        };
        var batch = new Seedling
        {
            Plant          = plant,
            SeedId         = seedId,
            LandingDate    = plantedDate,
            LunarPhase     = moonPhase,
            SeedlingSource = PlantSources.FromSeeds,
            PlantPlace     = plantPlace,
            IsPlantedInBed = true, // партия уже в грядке — не доступна к повторной посадке
            SeedlingInfos  = [info]
        };
        if (weightBased)
            batch.Weight = amount;
        else
            batch.Quantity = (int)Math.Round(amount);

        uow.Repository<Seedling>().Add(batch);
        return info;
    }
}
