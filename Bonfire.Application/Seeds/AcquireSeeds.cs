using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Bonfire.Application.Seeds;

public sealed record AcquireSeedsFromViewModel(SeedAcquisitionValues Values);

public abstract record AcquisitionResultFromViewModel
{
    private AcquisitionResultFromViewModel(int seedId) => SeedId = seedId;
    public int SeedId { get; }

    public sealed record Created : AcquisitionResultFromViewModel
    {
        public Created(int seedId) : base(seedId)
        {
        }
    }

    public sealed record Replenished : AcquisitionResultFromViewModel
    {
        public Replenished(int seedId) : base(seedId)
        {
        }
    }
}

public interface ISeedAcquisitionWriter
{
    Task<Result<AcquisitionResultFromViewModel>> SaveAsync(SeedAcquisition acquisition, DateTime purchasedAt,
        CancellationToken cancellationToken);
}

public interface IAcquireSeeds
{
    Task<Result<AcquisitionResultFromViewModel>> ExecuteAsync(AcquireSeedsFromViewModel request,
        CancellationToken cancellationToken = default);
}

public sealed class AcquireSeeds(ISeedAcquisitionWriter writer, TimeProvider time) : IAcquireSeeds
{
    public async Task<Result<AcquisitionResultFromViewModel>> ExecuteAsync(AcquireSeedsFromViewModel request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return await SeedAcquisition.Create(request.Values).Match(
            acquisition => writer.SaveAsync(acquisition, time.GetLocalNow().DateTime, cancellationToken),
            error => Task.FromResult(Result.Failure<AcquisitionResultFromViewModel>(error))).ConfigureAwait(false);
    }
}