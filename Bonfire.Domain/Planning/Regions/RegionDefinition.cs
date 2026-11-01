using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Bonfire.Domain.Planning.Regions;

public sealed record RegionReference
{
    public Option<AnnualDateWindow> LastSpringFrost { get; init; } = Option.None<AnnualDateWindow>();
    public Option<AnnualDateWindow> FirstAutumnFrost { get; init; } = Option.None<AnnualDateWindow>();
    public Option<string> Note { get; init; } = Option.None<string>();
    public Option<string> Source { get; init; } = Option.None<string>();
    public static RegionReference Empty { get; } = new();
}

public sealed record RegionDefinition
{
    public LibraryName Name { get; }
    public string TimeZoneId { get; }
    public TimeZoneInfo TimeZone { get; }
    public RegionReference Reference { get; }
    public bool IsArchived { get; }

    private RegionDefinition(LibraryName name, TimeZoneInfo timeZone, RegionReference reference, bool isArchived)
        => (Name, TimeZoneId, TimeZone, Reference, IsArchived) = (name, timeZone.Id, timeZone, reference, isArchived);

    public static Result<RegionDefinition> Create(string name, string timeZoneId, RegionReference reference, bool isArchived = false)
    {
        ArgumentNullException.ThrowIfNull(reference);
        return LibraryName.Create(name).Bind(validName =>
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
                return Result.Failure<RegionDefinition>("Не указан часовой пояс региона");
            try
            {
                return Result.Success(new RegionDefinition(validName, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId), reference, isArchived));
            }
            catch (TimeZoneNotFoundException)
            {
                return Result.Failure<RegionDefinition>("Часовой пояс региона не найден");
            }
            catch (InvalidTimeZoneException)
            {
                return Result.Failure<RegionDefinition>("Недопустимые правила часового пояса региона");
            }
        });
    }
}
