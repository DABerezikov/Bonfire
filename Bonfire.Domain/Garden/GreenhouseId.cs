using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Garden;

public sealed record GreenhouseId
{
    public int Value { get; }
    private GreenhouseId(int value) => Value = value;

    public static Result<GreenhouseId> Create(int value) => value > 0
        ? Result.Success(new GreenhouseId(value))
        : Result.Failure<GreenhouseId>("Некорректный идентификатор теплицы");
}
