using System.Collections.Immutable;
using System.Threading.Tasks;
using Bonfire.Services.Interfaces;
using MoonCalendar;
using Bonfire.Domain.Functional;
using Bonfire.Application.Seedlings;

namespace Bonfire.Services;

internal class SeedlingsService(MoonPhase lunar, ISowSeeds sowing, IDeleteSeedling deletion, IChangeSeedling changes, IEditSeedling editing, ISeedlingReader reader) : ISeedlingsService
{
    public Task<Result<ImmutableArray<SeedlingReadFromViewModel>>> ReadSeedlingsAsync() => reader.ReadAsync(default);
    public Task<Result<Option<SeedlingReadFromViewModel>>> ReadSeedlingAsync(int id) => reader.ReadAsync(id, default);

    public MoonPhase Lunar { get; } = lunar;

    public Task<Result<SowingReceipt>> SowAsync(SowingRequest request)
        => sowing.ExecuteAsync(request);

    public Task<Result<SeedlingDeletionResultFromViewModel>> DeleteAsync(DeleteSeedlingFromViewModel request)
        => deletion.ExecuteAsync(request);

    public Task<Result<SeedlingChangeResultFromViewModel>> ChangeAsync(ChangeSeedlingFromViewModel request)
        => changes.ExecuteAsync(request);

    public Task<Result<SeedlingEditResultFromViewModel>> EditAsync(EditSeedlingFromViewModel request)
        => editing.ExecuteAsync(request);

}
