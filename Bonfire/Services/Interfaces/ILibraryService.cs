using System.Threading.Tasks;
using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Models;

namespace Bonfire.Services.Interfaces;

public interface ILibraryService
{
    Task<Result<SortReadFromViewModel>> GetSortAsync(int id);
    Task<Result<CultureDetailsFromViewModel>> GetCultureAsync(int id);
    Task<Result<ProducerDetailsFromViewModel>> GetProducerAsync(int id);
    Task<Result<SortDetailsFromViewModel>> UpdateSortAsync(SortEditModel model);
    Task<Result<CultureDetailsFromViewModel>> UpdateCultureAsync(CultureEditModel model);
    Task<Result<ProducerDetailsFromViewModel>> UpdateProducerAsync(int id, string name);
}