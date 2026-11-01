using System.Collections.Immutable;
using System.Threading.Tasks;
using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;

namespace Bonfire.Services.Interfaces;

public interface ISeedlingsService
{
    ILunarCalendar Lunar { get; }
    Task<Result<SowingReceipt>> SowAsync(SowingRequest request);
    Task<Result<SeedlingDeletionResultFromViewModel>> DeleteAsync(DeleteSeedlingFromViewModel request);
    Task<Result<SeedlingChangeResultFromViewModel>> ChangeAsync(ChangeSeedlingFromViewModel request);
    Task<Result<SeedlingEditResultFromViewModel>> EditAsync(EditSeedlingFromViewModel request);
    Task<Result<ImmutableArray<SeedlingReadFromViewModel>>> ReadSeedlingsAsync();
    Task<Result<Option<SeedlingReadFromViewModel>>> ReadSeedlingAsync(int id);
}