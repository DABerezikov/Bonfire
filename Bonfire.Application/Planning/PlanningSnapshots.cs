using System.Collections.Immutable;
using Bonfire.Domain.Planning;

namespace Bonfire.Application.Planning;

/// <summary>Независимый снимок для представления; не содержит EF/WPF или фактических посевов.</summary>
public sealed record PlannedBatchFromViewModel(PlannedBatch Snapshot);

public sealed record SeasonalPlantingPlanFromViewModel(
    int Id, int SeasonYear, int RegionId, ImmutableArray<PlannedBatchFromViewModel> Batches);
