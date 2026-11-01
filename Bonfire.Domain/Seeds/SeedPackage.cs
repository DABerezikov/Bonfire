using Bonfire.Domain.Functional;

namespace Bonfire.Domain.Seeds;

public sealed record SeedPackageValues
{
    public double WeightPack { get; init; }
    public double QuantityPack { get; init; }
    public double AmountSeeds { get; init; }
    public Option<double> AmountSeedsWeight { get; init; } = Option.None<double>();
    public decimal CostPack { get; init; }
    public DateTime PurchaseDate { get; init; }
    public DateTime ExpirationDate { get; init; }
    public Option<string> Note { get; init; } = Option.None<string>();
    public Option<string> SeedSource { get; init; } = Option.None<string>();
    public Option<string> DisposeComment { get; init; } = Option.None<string>();
}

public sealed class SeedPackage
{
    private SeedPackage(SeedPackageValues values) => Values = values;
    public SeedPackageValues Values { get; }

    public static Result<SeedPackage> Create(SeedPackageValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.CostPack < 0
            ? Result.Failure<SeedPackage>("Стоимость упаковки не может быть отрицательной")
            : SeedAmount.Create(values.WeightPack, SeedUnit.Grams).Bind(_ =>
                SeedAmount.Create(values.QuantityPack, SeedUnit.Pieces).Bind(_ =>
                    SeedAmount.Create(values.AmountSeeds, SeedUnit.Pieces).Bind(_ =>
                        values.AmountSeedsWeight.Match(
                            weight => SeedAmount.Create(weight, SeedUnit.Grams)
                                .Bind(_ => Result.Success(new SeedPackage(values))),
                            () => Result.Success(new SeedPackage(values))))));
    }

    public Result<SeedPackage> WithDetails(SeedPackageValues values) => Create(values);

    public Result<SeedPackage> WithReturned(SeedAmount returned)
    {
        ArgumentNullException.ThrowIfNull(returned);
        if (returned.Value == 0) return WithDetails(Values);

        var available = returned.Unit == SeedUnit.Pieces
            ? SeedAmount.Create(Values.AmountSeeds, SeedUnit.Pieces)
            : Values.AmountSeedsWeight.Match(
                weight => SeedAmount.Create(weight, SeedUnit.Grams),
                () => Result.Failure<SeedAmount>("Вес остатка семян не указан"));

        return available.Bind(amount => new SeedStock(amount).WithReturned(returned))
            .Bind(stock => WithDetails(returned.Unit == SeedUnit.Pieces
                ? Values with { AmountSeeds = stock.Available.Value }
                : Values with { AmountSeedsWeight = Option.Some(stock.Available.Value) }));
    }
}