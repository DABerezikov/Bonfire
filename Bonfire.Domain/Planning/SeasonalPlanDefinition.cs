using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning.Regions;

namespace Bonfire.Domain.Planning;

public sealed record SeasonalPlanDefinition
{
    private SeasonalPlanDefinition(LibraryName name, int year, RegionId regionId, string regionName, string timeZoneId)
        => (Name, SeasonYear, RegionId, RegionName, TimeZoneId) = (name, year, regionId, regionName, timeZoneId);

    public LibraryName Name { get; }
    public int SeasonYear { get; }
    public RegionId RegionId { get; }
    public string RegionName { get; }
    public string TimeZoneId { get; }

    public static Result<int> ValidateYear(int year) => year is >= 1 and <= 9999
        ? Result.Success(year) : Result.Failure<int>("Год сезона должен быть от 1 до 9999");

    public static Result<SeasonalPlanDefinition> Create(string name, int year, RegionId regionId, string regionName,
        string timeZoneId)
    {
        ArgumentNullException.ThrowIfNull(regionId);
        return ValidateYear(year).Bind(_ => LibraryName.Create(name)).Bind(validName => RegionDefinition
            .Create(regionName, timeZoneId, RegionReference.Empty)
            .Bind(region =>
                Result.Success(new SeasonalPlanDefinition(validName, year, regionId, region.Name.Value,
                    region.TimeZoneId))));
    }
}
