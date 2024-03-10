using System.Linq;
using System.Threading.Tasks;
using BonfireDB.Entities;
using MoonCalendar;

namespace Bonfire.Services.Interfaces;

public interface ISeedlingsService
{
    IQueryable<Seedling> Seedlings { get; }
    MoonPhase Lunar { get; }
    Task<Seedling> MakeASeedling(Seedling seedling);
    Task<Seedling> UpdateSeedling(Seedling seedling);
    Task<Seedling?> DeleteSeedling(Seedling? seedling);
    Task<SeedlingInfo> AddSeedlingInfo(SeedlingInfo info);



}