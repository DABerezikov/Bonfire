using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning.Regions;

namespace Bonfire.Domain.Planning;

public sealed record SeasonalPlanDefinition
{
    public LibraryName Name { get; }
    public int SeasonYear { get; }
    public RegionId RegionId { get; }
    public string RegionName { get; }
    public string TimeZoneId { get; }

    private SeasonalPlanDefinition(LibraryName name, int year, RegionId regionId, string regionName, string timeZoneId)
        => (Name, SeasonYear, RegionId, RegionName, TimeZoneId) = (name, year, regionId, regionName, timeZoneId);

    public static Result<SeasonalPlanDefinition> Create(string name, int year, RegionId regionId, string regionName, string timeZoneId)
    {
        ArgumentNullException.ThrowIfNull(regionId);
        if (year is < 1 or > 9999) return Result.Failure<SeasonalPlanDefinition>("Год сезона должен быть от 1 до 9999");
        return LibraryName.Create(name).Bind(validName => RegionDefinition.Create(regionName, timeZoneId, RegionReference.Empty)
            .Bind(region => Result.Success(new SeasonalPlanDefinition(validName, year, regionId, region.Name.Value, region.TimeZoneId))));
    }
}
