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
    private readonly IRepository<Plant> _Plants = plants;
    private readonly IRepository<PlantSort> _Sort = sort;
    private readonly IRepository<PlantCulture> _Culture = culture;
    private readonly IRepository<Producer> _Producer = producer;
    private readonly IRepository<Replanting> _Replantings = replantings;
    private readonly IRepository<Treatment> _Treatments = treatments;

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
}