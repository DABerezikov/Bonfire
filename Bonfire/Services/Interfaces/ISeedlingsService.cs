using System.Linq;
using BonfireDB.Entities;

namespace Bonfire.Services.Interfaces;

public interface ISeedlingsService
{
    IQueryable<Seedling> Seedlings { get; }

        

}