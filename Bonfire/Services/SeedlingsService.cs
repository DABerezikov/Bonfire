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
    private readonly IRepository<PlantSort> _sort = sort;
    private readonly IRepository<PlantCulture> _culture = culture;
    private readonly IRepository<Producer> _producer = producer;
    private readonly IRepository<Replanting> _replantings = replantings;
    private readonly IRepository<Treatment> _treatments = treatments;
    private readonly IRepository<SeedlingInfo>_seedlingsInfo = seedlingsInfo;

    public IQueryable<Seedling> Seedlings => seedlings.Items;

    public MoonPhase Lunar { get; } = lunar;

    public async Task<Seedling> MakeASeedling(Seedling seedling)
    {
        var info = seedling.SeedlingInfos;
        foreach (var seedlingInfo in info)
        {
            await seedlingsInfo.AddAsync(seedlingInfo);

        }
        return await seedlings.AddAsync(seedling);

    }

    public async Task<Seedling> UpdateSeedling(Seedling seedling)
    {

        await seedlings.UpdateAsync(seedling);
        return seedling;

    }

    public async Task<Seedling> DeleteSeedling(Seedling seedling)
    {

        await seedlings.RemoveAsync(seedling.Id);
        return seedling;

    }
    public async Task<SeedlingInfo> AddSeedlingInfo(SeedlingInfo info)
    {
        
        return await seedlingsInfo.AddAsync(info);
       

    }

    public async Task UpdateSeedlingInfo(SeedlingInfo info)
    {
        if (!(info.Replants?.Count > 0))  await seedlingsInfo.UpdateAsync(info);
        foreach (var replant in info.Replants.Where(replant => replant.Id == 0))
        {
            await replantings.AddAsync(replant);
        }
        await seedlingsInfo.UpdateAsync(info);


    }

    public void InvertAutoSave()
    {
        plants.AutoSaveChanges= !plants.AutoSaveChanges;
        sort.AutoSaveChanges = !sort.AutoSaveChanges;
        seedlings.AutoSaveChanges = !seedlings.AutoSaveChanges;
        culture.AutoSaveChanges = !culture.AutoSaveChanges;
        producer.AutoSaveChanges = !producer.AutoSaveChanges;
        seedlingsInfo.AutoSaveChanges = !seedlingsInfo.AutoSaveChanges;
        replantings.AutoSaveChanges = !replantings.AutoSaveChanges;
        treatments.AutoSaveChanges = !treatments.AutoSaveChanges;
    }
}