using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Garden;

public sealed record GardenElementId
{
    public int Value { get; }
    private GardenElementId(int value) => Value = value;

    public static Result<GardenElementId> Create(int value) => value > 0
        ? Result.Success(new GardenElementId(value))
        : Result.Failure<GardenElementId>("Некорректный идентификатор элемента");
}
