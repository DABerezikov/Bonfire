using System.Threading.Tasks;
using Bonfire.Models;
using BonfireDB.Entities;
using BonfireDB.Entities.Base;

namespace Bonfire.Services.Interfaces;

public interface ILibraryService
{
    Task<Result<PlantSort>> GetSortAsync(int id);
    Task<Result<PlantCulture>> GetCultureAsync(int id);
    Task<Result<Producer>> GetProducerAsync(int id);
    Task<Result<PlantSort>> UpdateSortAsync(SortEditModel model);
    Task<Result<PlantCulture>> UpdateCultureAsync(CultureEditModel model);
    Task<Result<Producer>> UpdateProducerAsync(int id, string name);
}
