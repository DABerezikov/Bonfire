using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Libraries;

public sealed record LibraryName
{
    private LibraryName(string value) => Value = value;
    public string Value { get; }

    public static Result<LibraryName> Create(string value) => string.IsNullOrWhiteSpace(value)
        ? Result.Failure<LibraryName>("Название не может быть пустым")
        : Result.Success(new LibraryName(value));
}