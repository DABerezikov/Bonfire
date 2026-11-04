using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Seeds;

public sealed record SeedId
{
    private SeedId(int value) => Value = value;
    public int Value { get; }

    public static Result<SeedId> Create(int value) => value > 0
        ? Result.Success(new SeedId(value))
        : Result.Failure<SeedId>("Некорректный идентификатор семян");
}