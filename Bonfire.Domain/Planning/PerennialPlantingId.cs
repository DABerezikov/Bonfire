using Bonfire.Domain.Functional;
namespace Bonfire.Domain.Planning;
public sealed record PerennialPlantingId
{
    public int Value { get; }
    private PerennialPlantingId(int value) => Value = value;
    public static Result<PerennialPlantingId> Create(int value) => value > 0 ? Result.Success(new PerennialPlantingId(value))
        : Result.Failure<PerennialPlantingId>("Неверный ID многолетней посадки");
}
