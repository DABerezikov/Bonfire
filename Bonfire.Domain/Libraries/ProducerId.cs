using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Libraries;

public sealed record ProducerId
{
    private ProducerId(int value) => Value = value;
    public int Value { get; }

    public static Result<ProducerId> Create(int value) => value > 0
        ? Result.Success(new ProducerId(value))
        : Result.Failure<ProducerId>("Некорректный идентификатор производителя");
}