using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Libraries;

public sealed record LibraryName
{
    public string Value { get; }
    private LibraryName(string value) => Value = value;

    public static Result<LibraryName> Create(string value) => string.IsNullOrWhiteSpace(value)
        ? Result.Failure<LibraryName>("Название не может быть пустым")
        : Result.Success(new LibraryName(value));
}
