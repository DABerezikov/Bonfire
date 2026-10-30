using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;

namespace Bonfire.Domain.Seedlings;

public sealed record SeedlingId
{
    public int Value { get; }
    private SeedlingId(int value) => Value = value;
    public static Result<SeedlingId> Create(int value) => value > 0
        ? Result.Success(new SeedlingId(value))
        : Result.Failure<SeedlingId>("Некорректный идентификатор рассады");
}

public abstract record SeedlingRemoval
{
    private SeedlingRemoval() { }

    public sealed record Discard : SeedlingRemoval;

    public sealed record CorrectSowing : SeedlingRemoval
    {
        public Result<SeedPackage> ReturnTo(SeedPackage package, double quantity, double weight)
        {
            ArgumentNullException.ThrowIfNull(package);
            return SeedAmount.Create(quantity, SeedUnit.Pieces).Bind(pieces =>
                SeedAmount.Create(weight, SeedUnit.Grams).Bind(grams =>
                    package.WithReturned(pieces).Bind(updated => updated.WithReturned(grams))));
        }
    }
}
