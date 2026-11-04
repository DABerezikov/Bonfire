using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Libraries;

public sealed record CultureId
{
    private CultureId(int value) => Value = value;
    public int Value { get; }

    public static Result<CultureId> Create(int value) => value > 0
        ? Result.Success(new CultureId(value))
        : Result.Failure<CultureId>("Некорректный идентификатор культуры");
}