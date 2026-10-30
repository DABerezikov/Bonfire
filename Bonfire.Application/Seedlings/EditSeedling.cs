using Bonfire.Domain.Functional;
using Bonfire.Domain.Seedlings;

namespace Bonfire.Application.Seedlings;

public sealed record EditSeedlingFromViewModel(int SeedlingId, SeedlingDetailsValues Details, IReadOnlyList<SeedlingEntryEdit> Entries);
public sealed record SeedlingEditResultFromViewModel(SeedlingDetails Details, SeedlingChangeResultFromViewModel Change);

public interface ISeedlingEditWriter
{
    Task<Result<SeedlingEditResultFromViewModel>> SaveAsync(SeedlingEdit edit, CancellationToken cancellationToken);
}

public interface IEditSeedling
{
    Task<Result<SeedlingEditResultFromViewModel>> ExecuteAsync(EditSeedlingFromViewModel request, CancellationToken cancellationToken = default);
}

public sealed class EditSeedling(ISeedlingEditWriter writer, ILunarCalendar calendar) : IEditSeedling
{
    public Task<Result<SeedlingEditResultFromViewModel>> ExecuteAsync(EditSeedlingFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Details);
        ArgumentNullException.ThrowIfNull(request.Entries);
        cancellationToken.ThrowIfCancellationRequested();
        return request.Details.LandingDate.Match(date =>
            SeedlingDetails.Create(request.Details with { LunarPhase = calendar.GetPhase(date) })
                .Bind(details => SeedlingEdit.Create(request.SeedlingId, details, request.Entries))
                .Match(edit => writer.SaveAsync(edit, cancellationToken),
                    error => Task.FromResult(Result.Failure<SeedlingEditResultFromViewModel>(error))),
            () => Task.FromResult(Result.Failure<SeedlingEditResultFromViewModel>("Укажите дату посева")));
    }
}
