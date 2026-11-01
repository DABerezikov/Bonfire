using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning;
using Bonfire.Domain.Planning.GrowingProfiles;
using Bonfire.Domain.Planning.Regions;

namespace Bonfire.Application.Planning.Libraries;

public sealed record SaveRegionFromViewModel(Option<int> Id, string Name, string TimeZoneId, RegionReference Reference, bool IsArchived);
public sealed record SaveCultureProfileFromViewModel(int CultureId, GrowingProfileParameters Parameters, PlanningExtras Extras);
public sealed record SaveSortSettingsFromViewModel(int SortId, PlanningExtras Extras);
public sealed record SaveRegionalWindowFromViewModel(int RegionId, int CultureId, GrowingLocation Location,
    PlantPlacement Placement, RegionalWindowPurpose Purpose, AnnualDateWindow Window, Option<string> Source, Option<string> Note);

public interface ISavePlanningLibraries
{
    Task<Result<RegionFromViewModel>> SaveRegionAsync(SaveRegionFromViewModel request, CancellationToken cancellationToken = default);
    Task<Result<CultureGrowingProfile>> SaveCultureProfileAsync(SaveCultureProfileFromViewModel request, CancellationToken cancellationToken = default);
    Task<Result<SortPlanningSettings>> SaveSortSettingsAsync(SaveSortSettingsFromViewModel request, CancellationToken cancellationToken = default);
    Task<Result<RegionalPlantingWindow>> SaveRegionalWindowAsync(SaveRegionalWindowFromViewModel request, CancellationToken cancellationToken = default);
}

public sealed class SavePlanningLibraries(IPlanningLibraryWriter writer) : ISavePlanningLibraries
{
    public Task<Result<RegionFromViewModel>> SaveRegionAsync(SaveRegionFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return request.Id.Match(value => RegionId.Create(value).Bind(id => Result.Success(Option.Some(id))),
            () => Result.Success(Option.None<RegionId>())).Bind(id =>
                RegionDefinition.Create(request.Name, request.TimeZoneId, request.Reference, request.IsArchived)
                    .Bind(region => Result.Success((Id: id, Definition: region))))
            .Match(value => writer.SaveRegionAsync(value.Id, value.Definition, cancellationToken),
                error => Task.FromResult(Result.Failure<RegionFromViewModel>(error)));
    }

    public Task<Result<CultureGrowingProfile>> SaveCultureProfileAsync(SaveCultureProfileFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return CultureId.Create(request.CultureId).Bind(id => CultureGrowingProfile.Create(id, request.Parameters, request.Extras))
            .Match(value => writer.SaveCultureProfileAsync(value, cancellationToken),
                error => Task.FromResult(Result.Failure<CultureGrowingProfile>(error)));
    }

    public Task<Result<SortPlanningSettings>> SaveSortSettingsAsync(SaveSortSettingsFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return SortId.Create(request.SortId).Bind(id => SortPlanningSettings.Create(id, request.Extras))
            .Match(value => writer.SaveSortSettingsAsync(value, cancellationToken),
                error => Task.FromResult(Result.Failure<SortPlanningSettings>(error)));
    }

    public Task<Result<RegionalPlantingWindow>> SaveRegionalWindowAsync(SaveRegionalWindowFromViewModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return RegionId.Create(request.RegionId).Bind(region => CultureId.Create(request.CultureId).Bind(culture =>
            GrowingConditions.Create(request.Location, request.Placement).Bind(conditions =>
                RegionalPlantingWindow.Create(region, culture, conditions, request.Purpose, request.Window, request.Source, request.Note))))
            .Match(value => writer.SaveRegionalWindowAsync(value, cancellationToken),
                error => Task.FromResult(Result.Failure<RegionalPlantingWindow>(error)));
    }
}
