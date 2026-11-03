using Bonfire.Application.Caching;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Bonfire.Application.Seedlings;
using Bonfire.Domain.Functional;
using Bonfire.Services.Interfaces;

namespace Bonfire.Services;

internal class SeedlingsService(
    ILunarCalendar lunar,
    ISowSeeds sowing,
    IDeleteSeedling deletion,
    IChangeSeedling changes,
    IEditSeedling editing,
    ISeedlingReader reader,
    IReadModelCache cache) : ISeedlingsService
{
    public long ReadVersion => cache.Version(ReadArea.Seedlings | ReadArea.Catalog);

    public Task<Result<ImmutableArray<SeedlingReadFromViewModel>>> ReadSeedlingsAsync() => cache.ReadAsync(ReadArea.Seedlings | ReadArea.Catalog, nameof(ReadSeedlingsAsync), reader.ReadAsync);
    public Task<Result<Option<SeedlingReadFromViewModel>>> ReadSeedlingAsync(int id) => cache.ReadAsync(ReadArea.Seedlings | ReadArea.Catalog, (nameof(ReadSeedlingAsync), id), token => reader.ReadAsync(id, token));

    public ILunarCalendar Lunar { get; } = lunar;

    public Task<Result<SowingReceipt>> SowAsync(SowingRequest request)
        => sowing.ExecuteAsync(request);

    public Task<Result<SeedlingDeletionResultFromViewModel>> DeleteAsync(DeleteSeedlingFromViewModel request)
        => deletion.ExecuteAsync(request);

    public Task<Result<SeedlingChangeResultFromViewModel>> ChangeAsync(ChangeSeedlingFromViewModel request)
        => changes.ExecuteAsync(request);

    public Task<Result<SeedlingEditResultFromViewModel>> EditAsync(EditSeedlingFromViewModel request)
        => editing.ExecuteAsync(request);
}