using System.Collections.Immutable;
using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;

namespace Bonfire.Application.Seeds;

public sealed record SeedEditorOptionsFromViewModel(
    ImmutableArray<string> Colors,
    ImmutableArray<string> Maturities,
    ImmutableArray<ProducerDetailsFromViewModel> Producers)
{
    public Option<SeedCatalogFromViewModel> Catalog { get; init; } = Option.None<SeedCatalogFromViewModel>();
}

public sealed record CatalogSortFromViewModel(int Id, string Name, int ProducerId, ImmutableArray<int> CultureIds);
public sealed record SeedCatalogFromViewModel(
    ImmutableArray<CultureDetailsFromViewModel> Cultures,
    ImmutableArray<CatalogSortFromViewModel> Sorts,
    ImmutableArray<ProducerDetailsFromViewModel> Producers);

public interface ISeedEditorOptionsReader
{
    Task<Result<SeedEditorOptionsFromViewModel>> ReadAsync(CancellationToken cancellationToken);
}
