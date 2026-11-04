using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Seeds;

public enum SeedUnit
{
    Pieces,
    Grams
}

public sealed record SeedAmount
{
    private SeedAmount(double value, SeedUnit unit) => (Value, Unit) = (value, unit);
    public double Value { get; }
    public SeedUnit Unit { get; }

    public static Result<SeedAmount> Create(double value, SeedUnit unit) =>
        !Enum.IsDefined(unit)
            ? Result.Failure<SeedAmount>("Неизвестная единица семян")
            : !double.IsFinite(value) || value < 0
                ? Result.Failure<SeedAmount>("Количество семян должно быть конечным и неотрицательным")
                : Result.Success(new SeedAmount(value, unit));
}