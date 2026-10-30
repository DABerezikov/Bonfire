using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Garden;

public sealed record GardenId
{
    public int Value { get; }
    private GardenId(int value) => Value = value;

    public static Result<GardenId> Create(int value) => value > 0
        ? Result.Success(new GardenId(value))
        : Result.Failure<GardenId>("Некорректный идентификатор участка");
}
