using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Garden;

public sealed record GardenId
{
    private GardenId(int value) => Value = value;
    public int Value { get; }

    public static Result<GardenId> Create(int value) => value > 0
        ? Result.Success(new GardenId(value))
        : Result.Failure<GardenId>("Некорректный идентификатор участка");
}