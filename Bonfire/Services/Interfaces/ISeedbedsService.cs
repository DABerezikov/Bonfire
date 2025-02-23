using BonfireDB.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace Bonfire.Services.Interfaces;

public interface ISeedbedsService
{
    IQueryable<Seedbed> Seedbeds { get; }
    Task<Seedbed> MakeASeedbed(Seedbed seedbed);
    Task<Seedbed> DeleteSeedbed(Seedbed seedbed);
}