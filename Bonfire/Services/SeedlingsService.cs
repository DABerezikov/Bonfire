using System.Linq;
using System.Threading.Tasks;
using Bonfire.Services.Interfaces;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;
using MoonCalendar;

namespace Bonfire.Services;



internal class SeedlingsService : ISeedlingsService
{
    private readonly IRepository<Plant> _plants;
    private readonly IRepository<Seedling> _seedlings;
    private readonly IRepository<PlantSort> _sort;
    private readonly IRepository<PlantCulture> _culture;
    private readonly IRepository<Producer> _producer;
    private readonly IRepository<SeedlingInfo> _seedlingsInfo;
    private readonly IRepository<Replanting> _replantings;
    private readonly IRepository<Treatment> _treatments;

    public IQueryable<Seedling> Seedlings => _seedlings.Items;

    public MoonPhase Lunar { get; }

    public SeedlingsService(IRepository<Plant> plants,
        IRepository<Seedling> seedlings,
        IRepository<PlantSort> sort,
        IRepository<PlantCulture> culture,
        IRepository<Producer> producer,
        IRepository<SeedlingInfo> seedlingsInfo,
        IRepository<Replanting> replantings,
        IRepository<Treatment> treatments,
        MoonPhase lunar)
    {
        _plants = plants;
        _seedlings = seedlings;
        _sort = sort;
        _culture = culture;
        _producer = producer;
        _seedlingsInfo = seedlingsInfo;
        _replantings = replantings;
        _treatments = treatments;
        Lunar = lunar;
    }

    public async Task<Seedling> MakeASeedling(Seedling seedling)
    {
        var seedlingsInfo = seedling.SeedlingInfos;
        foreach (var seedlingInfo in seedlingsInfo)
        {
            await _seedlingsInfo.AddAsync(seedlingInfo);

        }
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
}