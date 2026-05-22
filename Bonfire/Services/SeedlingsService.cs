using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bonfire.Services.Interfaces;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using Microsoft.EntityFrameworkCore;
using MoonCalendar;

namespace Bonfire.Services;

internal class SeedlingsService(IUnitOfWorkFactory uowFactory, MoonPhase lunar) : ISeedlingsService
{
    public MoonPhase Lunar { get; } = lunar;

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
