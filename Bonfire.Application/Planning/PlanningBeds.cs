using System.Collections.Immutable;
using Bonfire.Domain.Functional;

namespace Bonfire.Application.Planning;

public sealed record PlanningBedFromViewModel(int Id, string Name, double WidthPixels, double HeightPixels);
public interface IPlanningBedReader
{
    Task<Result<ImmutableArray<PlanningBedFromViewModel>>> ReadAsync(CancellationToken cancellationToken);
}
