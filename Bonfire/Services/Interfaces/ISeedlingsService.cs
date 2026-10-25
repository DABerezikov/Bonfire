using System.Collections.Immutable;
﻿using System.Collections.Generic;
using System.Threading.Tasks;
using BonfireDB.Entities;
using MoonCalendar;
using Bonfire.Domain.Functional;
using Bonfire.Application.Seedlings;

namespace Bonfire.Services.Interfaces;

public interface ISeedlingsService
{
    MoonPhase Lunar { get; }
    Task<Result<SowingReceipt>> SowAsync(SowingRequest request);
    Task<Result<SeedlingDeletionResultFromViewModel>> DeleteAsync(DeleteSeedlingFromViewModel request);
    Task<Result<SeedlingChangeResultFromViewModel>> ChangeAsync(ChangeSeedlingFromViewModel request);
    Task<Result<SeedlingEditResultFromViewModel>> EditAsync(EditSeedlingFromViewModel request);
    Task<Result<ImmutableArray<SeedlingReadFromViewModel>>> ReadSeedlingsAsync();
    Task<Result<Option<SeedlingReadFromViewModel>>> ReadSeedlingAsync(int id);
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
