using System.Collections.Generic;
using System.Threading.Tasks;
using BonfireDB.Entities;
using MoonCalendar;

namespace Bonfire.Services.Interfaces;

public interface ISeedlingsService
{
    MoonPhase Lunar { get; }
    Task<IReadOnlyList<Seedling>> GetAllSeedlingsAsync();
    Task<Seedling?> GetSeedlingAsync(int id);
    Task<Seedling> MakeASeedling(Seedling seedling);
    Task<Seedling> UpdateSeedling(Seedling seedling);
    Task<Seedling> DeleteSeedling(Seedling seedling);
    Task<SeedlingInfo> AddSeedlingInfo(SeedlingInfo info);
    Task UpdateSeedlingInfo(SeedlingInfo info);

    /// <summary>
    /// Помечает указанные записи всходов погибшими и сохраняет их вместе с рассадой
    /// одним сохранением (атомарно в рамках общего DbContext).
    /// </summary>
    Task MarkSeedlingInfosDeadAsync(Seedling seedling, IReadOnlyList<SeedlingInfo> infos, string? deathNote);
}