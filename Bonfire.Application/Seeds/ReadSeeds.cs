using System.Collections.Immutable;
using Bonfire.Application.Libraries;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Bonfire.Application.Seeds;

public sealed record PlantReadFromViewModel(
    int Id,
    CultureDetailsFromViewModel PlantCulture,
    SortReadFromViewModel PlantSort);

public sealed record SeedReadFromViewModel(
    int Id,
    int SeedsInfoId,
    PlantReadFromViewModel Plant,
    SeedPackageValues SeedsInfo);

public interface ISeedReader
{
    Task<Result<ImmutableArray<SeedReadFromViewModel>>> ReadAsync(CancellationToken cancellationToken);
}