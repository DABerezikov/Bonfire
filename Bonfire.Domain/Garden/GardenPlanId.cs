using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Garden;

public sealed record GardenPlanId
{
    private GardenPlanId(int value) => Value = value;
    public int Value { get; }

    public static Result<GardenPlanId> Create(int value) => value > 0
        ? Result.Success(new GardenPlanId(value))
        : Result.Failure<GardenPlanId>("Некорректный идентификатор плана");
}