using System.Collections.Immutable;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Planning.Regions;

namespace Bonfire.Application.Planning.Libraries;

public sealed record RegionFromViewModel(RegionId Id, RegionDefinition Definition);

public sealed record GrowingProfilesFromViewModel(
    Option<CultureGrowingProfile> Culture,
    Option<SortPlanningSettings> Sort);

public interface IPlanningLibraryReader
{
    Task<Result<ImmutableArray<RegionFromViewModel>>> ReadRegionsAsync(bool includeArchived,
        CancellationToken cancellationToken);

    Task<Result<ImmutableArray<RegionalPlantingWindow>>> ReadRegionalWindowsAsync(RegionId region, CultureId culture,
        CancellationToken cancellationToken);

    Task<Result<GrowingProfilesFromViewModel>> ReadProfilesAsync(CultureId culture, SortId sort,
        CancellationToken cancellationToken);
}

public interface IPlanningLibraryWriter
{
    Task<Result<RegionFromViewModel>> SaveRegionAsync(Option<RegionId> id, RegionDefinition definition,
        CancellationToken cancellationToken);

    Task<Result<CultureGrowingProfile>> SaveCultureProfileAsync(CultureGrowingProfile profile,
        CancellationToken cancellationToken);

    Task<Result<SortPlanningSettings>> SaveSortSettingsAsync(SortPlanningSettings settings,
        CancellationToken cancellationToken);

    Task<Result<RegionalPlantingWindow>> SaveRegionalWindowAsync(RegionalPlantingWindow window,
        CancellationToken cancellationToken);
}