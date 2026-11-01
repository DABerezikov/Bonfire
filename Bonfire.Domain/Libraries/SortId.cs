using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Libraries;

public sealed record SortId
{
    private SortId(int value) => Value = value;
    public int Value { get; }

    public static Result<SortId> Create(int value) => value > 0
        ? Result.Success(new SortId(value))
        : Result.Failure<SortId>("Некорректный идентификатор сорта");
}