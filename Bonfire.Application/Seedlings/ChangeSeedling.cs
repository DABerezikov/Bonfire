using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;

namespace Bonfire.Application.Seedlings;

public sealed record ChangeSeedlingFromViewModel(int SeedlingId, SeedlingAction Action);
public sealed record ReplantingEntryFromViewModel(int Id, DateTime Date, Option<double> PotVolume, Option<string> Note);
public sealed record SeedlingEntryFromViewModel(int Id, int Number, Option<DateTime> GerminationDate, Option<bool> IsDead)
{
    public IReadOnlyList<ReplantingEntryFromViewModel> Replants { get; init; } = [];
}
public sealed record SeedlingChangeResultFromViewModel(int SeedlingId, IReadOnlyList<SeedlingEntryFromViewModel> Entries);

public interface ISeedlingChangeWriter
{
    Task<Result<SeedlingChangeResultFromViewModel>> ApplyAsync(SeedlingId id, SeedlingAction action, CancellationToken cancellationToken);
}

public interface IChangeSeedling
{
    Task<Result<SeedlingChangeResultFromViewModel>> ExecuteAsync(ChangeSeedlingFromViewModel request, CancellationToken cancellationToken = default);
}

public sealed class ChangeSeedling(ISeedlingChangeWriter writer) : IChangeSeedling
{
    public Task<Result<SeedlingChangeResultFromViewModel>> ExecuteAsync(ChangeSeedlingFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Action);
        cancellationToken.ThrowIfCancellationRequested();
        return SeedlingId.Create(request.SeedlingId).Match(
            id => writer.ApplyAsync(id, request.Action, cancellationToken),
            error => Task.FromResult(Result.Failure<SeedlingChangeResultFromViewModel>(error)));
    }
}
