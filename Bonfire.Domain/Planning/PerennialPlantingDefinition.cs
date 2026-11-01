using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;
using Bonfire.Domain.Planning.Regions;

namespace Bonfire.Domain.Planning;

public sealed record PerennialPlantingDefinition
{
    public string Name { get; }
    public int EstablishmentYear { get; }
    public CultureId CultureId { get; }
    public SortId SortId { get; }
    public string CultureName { get; }
    public string SortName { get; }
    public RegionId RegionId { get; }
    public string RegionName { get; }
    public string TimeZoneId { get; }
    public SourcedValue<LifeCycleProfile> Profile { get; }
    public Option<DateWindow> FirstEmergence { get; }
    private PerennialPlantingDefinition(string name, int establishmentYear, CultureId cultureId, SortId sortId, string cultureName, string sortName,
        RegionId regionId, string regionName, string timeZoneId, SourcedValue<LifeCycleProfile> profile, Option<DateWindow> firstEmergence)
        => (Name, EstablishmentYear, CultureId, SortId, CultureName, SortName, RegionId, RegionName, TimeZoneId, Profile, FirstEmergence)
            = (name, establishmentYear, cultureId, sortId, cultureName, sortName, regionId, regionName, timeZoneId, profile, firstEmergence);

    public static Result<PerennialPlantingDefinition> Create(string name, int establishmentYear, CultureId cultureId, SortId sortId, string cultureName, string sortName,
        RegionId regionId, string regionName, string timeZoneId, SourcedValue<LifeCycleProfile> profile, Option<DateWindow> firstEmergence)
    {
        if (establishmentYear is < 1 or > 9999 || profile.Value.Kind == LifeCycleKind.Annual || !Enum.IsDefined(profile.Source))
            return Result.Failure<PerennialPlantingDefinition>("Укажите год начала и двулетний/многолетний жизненный цикл");
        return LibraryName.Create(name).Bind(_ => LibraryName.Create(cultureName)).Bind(_ => LibraryName.Create(sortName))
            .Bind(_ => RegionDefinition.Create(regionName, timeZoneId, RegionReference.Empty)).Bind(_ => profile.Value.Validate())
            .Bind(_ => Result.Success(new PerennialPlantingDefinition(name, establishmentYear, cultureId, sortId, cultureName, sortName, regionId,
                regionName, timeZoneId, profile, firstEmergence)));
    }
}
