using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Garden;

public sealed record GardenPlanId
{
    public int Value { get; }
    private GardenPlanId(int value) => Value = value;

    public static Result<GardenPlanId> Create(int value) => value > 0
        ? Result.Success(new GardenPlanId(value))
        : Result.Failure<GardenPlanId>("Некорректный идентификатор плана");
}
