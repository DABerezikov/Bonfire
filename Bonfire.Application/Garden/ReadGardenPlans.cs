using System.Collections.Immutable;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Garden;

namespace Bonfire.Application.Garden;

public sealed record GardenReadFromViewModel(
    GardenPropertiesResultFromViewModel Properties,
    ImmutableArray<ElementReadFromViewModel> Elements,
    ImmutableArray<GreenhouseReadFromViewModel> Greenhouses);

public sealed record ElementReadFromViewModel(
    ElementPropertiesResultFromViewModel Properties,
    string ElementType,
    double Rotation,
    string StateTypeName,
    bool IsLocked,
    int GridRows,
    int GridColumns,
    Option<string> Shape,
    ImmutableArray<GridSpotFromViewModel> Spots);

public sealed record GreenhouseReadFromViewModel(
    GreenhousePropertiesResultFromViewModel Properties,
    CanvasRectangle Rectangle,
    double Rotation,
    string StateTypeName,
    bool IsLocked,
    ImmutableArray<ElementReadFromViewModel> Elements);

public interface IGardenReader
{
    Task<Result<ImmutableArray<GardenPlanResultFromViewModel>>> ReadPlansAsync(CancellationToken cancellationToken);

    Task<Result<ImmutableArray<GardenReadFromViewModel>>> ReadGardensAsync(GardenPlanId planId,
        CancellationToken cancellationToken);
}