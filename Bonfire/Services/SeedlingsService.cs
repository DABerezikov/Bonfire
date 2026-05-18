using System.Linq;
using System.Threading.Tasks;
using Bonfire.Services.Interfaces;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using MoonCalendar;

namespace Bonfire.Services;

internal class SeedlingsService(
    IRepository<Plant> plants,
    IRepository<Seedling> seedlings,
    IRepository<PlantSort> sort,
    IRepository<PlantCulture> culture,
    IRepository<Producer> producer,
    IRepository<SeedlingInfo> seedlingsInfo,
    IRepository<Replanting> replantings,
    IRepository<Treatment> treatments,
    MoonPhase lunar)
    : ISeedlingsService
{
    private readonly IRepository<Plant> _plants = plants;
    private readonly IRepository<Seedling> _seedlings = seedlings;
    private readonly IRepository<PlantSort> _sort = sort;
    private readonly IRepository<PlantCulture> _culture = culture;
    private readonly IRepository<Producer> _producer = producer;
    private readonly IRepository<SeedlingInfo> _seedlingsInfo = seedlingsInfo;
    private readonly IRepository<Replanting> _replantings = replantings;
    private readonly IRepository<Treatment> _treatments = treatments;

    public IQueryable<Seedling> Seedlings => _seedlings.Items;

    public MoonPhase Lunar { get; } = lunar;

    public async Task<Seedling> MakeASeedling(Seedling seedling)
    {
        var info = seedling.SeedlingInfos;
        foreach (var seedlingInfo in info)
            await _seedlingsInfo.AddAsync(seedlingInfo);
        return await _seedlings.AddAsync(seedling);
    }

    public async Task<Seedling> UpdateSeedling(Seedling seedling)
    {
        await _seedlings.UpdateAsync(seedling);
        return seedling;
    }

    public async Task<Seedling> DeleteSeedling(Seedling seedling)
    {
        await _seedlings.RemoveAsync(seedling.Id);
        return seedling;
    }

    public async Task<SeedlingInfo> AddSeedlingInfo(SeedlingInfo info)
    {
        return await _seedlingsInfo.AddAsync(info);
    }

    public async Task UpdateSeedlingInfo(SeedlingInfo info)
    {
        if (!(info.Replants?.Count > 0))
        {
            await _seedlingsInfo.UpdateAsync(info);
            return;
        }
        foreach (var replant in info.Replants!.Where(replant => replant.Id == 0))
            await _replantings.AddAsync(replant);
        await _seedlingsInfo.UpdateAsync(info);
    }

    public void InvertAutoSave()
    {
        _plants.AutoSaveChanges = !_plants.AutoSaveChanges;
        _sort.AutoSaveChanges = !_sort.AutoSaveChanges;
        _seedlings.AutoSaveChanges = !_seedlings.AutoSaveChanges;
        _culture.AutoSaveChanges = !_culture.AutoSaveChanges;
        _producer.AutoSaveChanges = !_producer.AutoSaveChanges;
        _seedlingsInfo.AutoSaveChanges = !_seedlingsInfo.AutoSaveChanges;
        _replantings.AutoSaveChanges = !_replantings.AutoSaveChanges;
        _treatments.AutoSaveChanges = !_treatments.AutoSaveChanges;
    }
}
