using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Seeds;

public sealed class SeedStock
{
    public SeedStock(SeedAmount available)
    {
        ArgumentNullException.ThrowIfNull(available);
        Available = available;
    }

    public SeedAmount Available { get; }

    public Result<SeedStock> WithReturned(SeedAmount returned)
    {
        ArgumentNullException.ThrowIfNull(returned);
        return returned.Unit != Available.Unit
            ? Result.Failure<SeedStock>("Единицы остатка и возврата должны совпадать")
            : SeedAmount.Create(Available.Value + returned.Value, Available.Unit)
                .Bind(amount => Result.Success(new SeedStock(amount)));
    }

    public Result<SowingChange> SowUpTo(SeedAmount requested)
    {
        ArgumentNullException.ThrowIfNull(requested);
        if (requested.Unit != Available.Unit)
            return Result.Failure<SowingChange>("Единицы остатка и посева должны совпадать");
        var amount = Math.Min(requested.Value, Available.Value);
        return SeedAmount.Create(requested.Unit == SeedUnit.Pieces ? Math.Round(amount) : amount, requested.Unit)
            .Bind(Sow);
    }

    public Result<SowingChange> Sow(SeedAmount requested)
    {
        ArgumentNullException.ThrowIfNull(requested);
        return requested.Unit != Available.Unit
            ? Result.Failure<SowingChange>("Единицы остатка и посева должны совпадать")
            : requested.Value <= 0
                ? Result.Failure<SowingChange>("Количество для посева должно быть положительным")
                : requested.Value > Available.Value
                    ? Result.Failure<SowingChange>("Недостаточно семян для посева")
                    : SeedAmount.Create(Available.Value - requested.Value, Available.Unit)
                        .Bind(remaining => Result.Success(new SowingChange(new SeedStock(remaining), requested)));
    }
}

public sealed class SowingChange
{
    internal SowingChange(SeedStock remaining, SeedAmount sown) => (Remaining, Sown) = (remaining, sown);
    public SeedStock Remaining { get; }
    public SeedAmount Sown { get; }
}