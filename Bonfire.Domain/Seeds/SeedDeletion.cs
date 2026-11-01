using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Seeds;

public sealed class SeedDeletion
{
    private SeedDeletion(SeedId seedId, string reason) => (SeedId, Reason) = (seedId, reason);
    public SeedId SeedId { get; }
    public string Reason { get; }

    public static Result<SeedDeletion> Create(int seedId, string reason)
        => SeedId.Create(seedId).Bind(id => reason is { } text
            ? Result.Success(new SeedDeletion(id, text.Trim()))
            : Result.Failure<SeedDeletion>("Причина удаления не передана"));

    public Result<SeedDeletion> WithReason(string reason) => Create(SeedId.Value, reason);
}