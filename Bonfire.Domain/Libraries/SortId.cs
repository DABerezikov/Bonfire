using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Libraries;

public sealed record SortId
{
    public int Value { get; }
    private SortId(int value) => Value = value;

    public static Result<SortId> Create(int value) => value > 0
        ? Result.Success(new SortId(value))
        : Result.Failure<SortId>("Некорректный идентификатор сорта");
}
