using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Bonfire.Domain.Planning.Regions;

public enum RegionalWindowPurpose
{
    Transplanting,
    DirectSowing
}

public sealed record RegionalPlantingWindow
{
    private RegionalPlantingWindow(RegionId regionId, CultureId cultureId, GrowingConditions conditions,
        RegionalWindowPurpose purpose, AnnualDateWindow window, Option<string> source, Option<string> note)
        => (RegionId, CultureId, Conditions, Purpose, Window, Source, Note)
            = (regionId, cultureId, conditions, purpose, window, source, note);

    public RegionId RegionId { get; }
    public CultureId CultureId { get; }
    public GrowingConditions Conditions { get; }
    public RegionalWindowPurpose Purpose { get; }
    public AnnualDateWindow Window { get; }
    public Option<string> Source { get; }
    public Option<string> Note { get; }

    public static Result<RegionalPlantingWindow> Create(RegionId regionId, CultureId cultureId,
        GrowingConditions conditions, RegionalWindowPurpose purpose, AnnualDateWindow window,
        Option<string> source, Option<string> note)
    {
        ArgumentNullException.ThrowIfNull(regionId);
        ArgumentNullException.ThrowIfNull(cultureId);
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(note);
        return Enum.IsDefined(purpose)
            ? Result.Success(new RegionalPlantingWindow(regionId, cultureId, conditions, purpose, window, source, note))
            : Result.Failure<RegionalPlantingWindow>("Неизвестное назначение справочного срока");
    }
}