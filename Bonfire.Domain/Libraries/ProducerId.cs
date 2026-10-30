using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Libraries;

public sealed record ProducerId
{
    public int Value { get; }
    private ProducerId(int value) => Value = value;

    public static Result<ProducerId> Create(int value) => value > 0
        ? Result.Success(new ProducerId(value))
        : Result.Failure<ProducerId>("Некорректный идентификатор производителя");
}
