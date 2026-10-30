using System.Collections.Immutable;
using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;

namespace Bonfire.Application.Seeds;

public sealed record SeedEditorOptionsFromViewModel(ImmutableArray<string> Colors,
    ImmutableArray<string> Maturities, ImmutableArray<ProducerDetailsFromViewModel> Producers);

public interface ISeedEditorOptionsReader
{
    Task<Result<SeedEditorOptionsFromViewModel>> ReadAsync(CancellationToken cancellationToken);
}
