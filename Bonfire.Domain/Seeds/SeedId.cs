using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Seeds;

public sealed record SeedId
{
    public int Value { get; }
    private SeedId(int value) => Value = value;
    public static Result<SeedId> Create(int value) => value > 0
        ? Result.Success(new SeedId(value))
        : Result.Failure<SeedId>("Некорректный идентификатор семян");
}
