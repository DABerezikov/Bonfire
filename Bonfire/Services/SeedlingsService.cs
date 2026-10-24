using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bonfire.Services.Interfaces;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;
using MoonCalendar;
using Bonfire.Domain.Functional;
using Bonfire.Models;
using Bonfire.Domain.Seeds;
using System;

namespace Bonfire.Services;

internal class SeedlingsService(IUnitOfWorkFactory uowFactory, MoonPhase lunar) : ISeedlingsService
{
    public MoonPhase Lunar { get; } = lunar;

    public async Task<Result<SowingReceipt>> SowAsync(SowingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Date == default || string.IsNullOrWhiteSpace(request.Source))
            return Result.Failure<SowingReceipt>("Укажите дату и источник рассады");

        await using var uow = uowFactory.Create();
        var seed = await uow.Repository<Seed>().GetAsync(request.SeedId);
        if (seed is null) return Result.Failure<SowingReceipt>("Пакет семян не найден");

        var available = request.Unit switch
        {
            SeedUnit.Pieces => SeedAmount.Create(seed.SeedsInfo.AmountSeeds, request.Unit),
            SeedUnit.Grams when seed.SeedsInfo.AmountSeedsWeight is double weight
                => SeedAmount.Create(weight, request.Unit),
            SeedUnit.Grams => Result.Failure<SeedAmount>("Вес остатка семян не указан"),
            _ => Result.Failure<SeedAmount>("Неизвестная единица семян")
        };
        var change = available.Bind(amount => SeedAmount.Create(request.Amount, request.Unit)
            .Bind(requested => new SeedStock(amount).Sow(requested)));

        return await change.Match(async sowing =>
        {
            var phase = Lunar.GetMoonPhase(request.Date);
            var place = string.IsNullOrWhiteSpace(request.Place) ? null : request.Place;
            var batch = new Seedling
            {
                Plant = seed.Plant, SeedId = seed.Id,
                Quantity = request.Unit == SeedUnit.Pieces ? sowing.Sown.Value : 0,
                Weight = request.Unit == SeedUnit.Grams ? sowing.Sown.Value : 0,
                LandingDate = request.Date, LunarPhase = phase,
                SeedlingSource = request.Source, PlantPlace = place,
                SeedlingInfos = [new SeedlingInfo
                {
                    LandingDate = request.Date, LunarPhase = phase,
                    SeedlingSource = request.Source, PlantPlace = place, SeedlingNumber = 0
                }]
            };
            if (request.Unit == SeedUnit.Pieces)
                seed.SeedsInfo.AmountSeeds = sowing.Remaining.Available.Value;
            else
                seed.SeedsInfo.AmountSeedsWeight = sowing.Remaining.Available.Value;

            await uow.Repository<Seedling>().AddAsync(batch);
            await uow.SaveChangesAsync();
            return Result.Success(new SowingReceipt(batch, seed));
        }, error => Task.FromResult(Result.Failure<SowingReceipt>(error)));
    }

    public async Task<IReadOnlyList<Seedling>> GetAllSeedlingsAsync()
    {
        await using var uow = uowFactory.Create();
        return await uow.Repository<Seedling>().Items.ToListAsync();
    }

    public async Task<Seedling?> GetSeedlingAsync(int id)
    {
        await using var uow = uowFactory.Create();
        return await uow.Repository<Seedling>().GetAsync(id);
    }

    public async Task<Seedling> MakeASeedling(Seedling seedling)
    {
        await using var uow = uowFactory.Create();
        // Attach всего графа: новая рассада и её записи (Id == 0) → Added,
        // существующее растение (Id != 0) → Unchanged. Связь «рассада → запись»
        // расставляет FK по навигации, поэтому отдельный Add записей не нужен.
        await uow.Repository<Seedling>().AddAsync(seedling);
        await uow.SaveChangesAsync();
        return seedling;
    }

    public async Task<Seedling> UpdateSeedling(Seedling seedling)
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<Seedling>().UpdateAsync(seedling);
        await uow.SaveChangesAsync();
        return seedling;
    }

    public async Task<Seedling> DeleteSeedling(Seedling seedling)
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<Seedling>().RemoveAsync(seedling.Id);
        await uow.SaveChangesAsync();
        return seedling;
    }

    public async Task<SeedlingInfo> AddSeedlingInfo(SeedlingInfo info)
    {
        await using var uow = uowFactory.Create();
        await uow.Repository<SeedlingInfo>().AddAsync(info);
        await uow.SaveChangesAsync();
        return info;
    }

    public async Task UpdateSeedlingInfo(SeedlingInfo info)
    {
        await using var uow = uowFactory.Create();
        // Update(graph) сам добавит новые пересадки (Id == 0 → Added) и расставит FK,
        // но добавляем их явно, чтобы порядок и состояние были предсказуемы.
        if (info.Replants?.Count > 0)
            foreach (var replant in info.Replants.Where(r => r.Id == 0))
                await uow.Repository<Replanting>().AddAsync(replant);
        await uow.Repository<SeedlingInfo>().UpdateAsync(info);
        await uow.SaveChangesAsync();
    }

    public async Task MarkSeedlingInfosDeadAsync(Seedling seedling, IReadOnlyList<SeedlingInfo> infos, string? deathNote)
    {
        await using var uow = uowFactory.Create();
        var infoRepo = uow.Repository<SeedlingInfo>();
        foreach (var info in infos)
        {
            info.IsDead = true;
            info.DeathNote = deathNote;
            await infoRepo.UpdateAsync(info);
        }
        // Все правки записей всходов и сама рассада сохраняются одним SaveChanges
        // в общем DbContext этого UoW — атомарно.
        await uow.Repository<Seedling>().UpdateAsync(seedling);
        await uow.SaveChangesAsync();
    }
}
