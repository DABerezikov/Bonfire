using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Libraries;

public sealed class SortLibraryEdit
{
    private SortLibraryEdit(SortId id, LibraryName name, Option<CultureId> cultureChange)
        => (Id, Name, CultureChange) = (id, name, cultureChange);

    public SortId Id { get; }
    public LibraryName Name { get; }
    public Option<CultureId> CultureChange { get; }

    public static Result<SortLibraryEdit> Create(int id, string name, Option<int> cultureChange)
    {
        ArgumentNullException.ThrowIfNull(cultureChange);
        return SortId.Create(id).Bind(identity => LibraryName.Create(name).Bind(validName =>
            LibraryName.Create(validName.Value.Trim()).Bind(trimmedName => cultureChange.Match(
                    value => CultureId.Create(value).Bind(culture => Result.Success(Option.Some(culture))),
                    () => Result.Success(Option.None<CultureId>()))
                .Bind(culture => Result.Success(new SortLibraryEdit(identity, trimmedName, culture))))));
    }

    public Result<SortLibraryEdit> WithDetails(string name, Option<int> cultureChange)
        => Create(Id.Value, name, cultureChange);
}