using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Libraries;

public enum SortOptionCategory { Color, Maturity }

public sealed record SortOptionName
{
    private SortOptionName(SortOptionCategory category, LibraryName name) { Category = category; Name = name; }
    public SortOptionCategory Category { get; }
    public LibraryName Name { get; }
    public static Result<SortOptionName> Create(SortOptionCategory category, string name)
        => category is not (SortOptionCategory.Color or SortOptionCategory.Maturity)
            ? Result.Failure<SortOptionName>("Неизвестный справочник")
            : LibraryName.Create(name).Bind(valid => LibraryName.Create(valid.Value.Trim())
                .Bind(value => Result.Success(new SortOptionName(category, value))));
}
