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
    private RegionDefinition(LibraryName name, TimeZoneInfo timeZone, RegionReference reference, bool isArchived)
        => (Name, TimeZoneId, TimeZone, Reference, IsArchived) = (name, timeZone.Id, timeZone, reference, isArchived);

    public LibraryName Name { get; }
    public string TimeZoneId { get; }
    public TimeZoneInfo TimeZone { get; }
    public RegionReference Reference { get; }
    public bool IsArchived { get; }

    public static Result<RegionDefinition> Create(string name, string timeZoneId, RegionReference reference,
        bool isArchived = false)
    {
        ArgumentNullException.ThrowIfNull(reference);
        return LibraryName.Create(name).Bind(validName =>
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
                return Result.Failure<RegionDefinition>("Не указан часовой пояс региона");
            return TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone)
                ? Result.Success(new RegionDefinition(validName, zone, reference, isArchived))
                : Result.Failure<RegionDefinition>("Часовой пояс региона не найден или содержит недопустимые правила");
        });
    }
}