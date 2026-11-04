using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Planning;

public sealed record PerennialPlantingId
{
    private PerennialPlantingId(int value) => Value = value;
    public int Value { get; }

    public static Result<PerennialPlantingId> Create(int value) => value > 0
        ? Result.Success(new PerennialPlantingId(value))
        : Result.Failure<PerennialPlantingId>("Неверный ID многолетней посадки");
}