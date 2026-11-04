using Bonfire.Domain.Functional;
using Bonfire.Domain.Libraries;

namespace Bonfire.Domain.Seeds;

public sealed record SeedAcquisitionValues(
    string Culture,
    string Sort,
    string Producer,
    string Class,
    string Source,
    SeedUnit Unit,
    double QuantityInPack,
    double PackCount,
    decimal CostPack,
    DateTime BestBy,
    Option<string> Note);

public sealed class SeedAcquisition
{
    private SeedAcquisition(SeedAcquisitionValues values, SeedAmount total) => (Values, Total) = (values, total);
    public SeedAcquisitionValues Values { get; }
    public SeedAmount Total { get; }

    public static Result<SeedAcquisition> Create(SeedAcquisitionValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(values.Note);
        if (string.IsNullOrWhiteSpace(values.Class) || string.IsNullOrWhiteSpace(values.Source) ||
            values.BestBy == default)
            return Result.Failure<SeedAcquisition>("Укажите класс, источник и срок годности семян");
        if (!double.IsFinite(values.PackCount) || values.PackCount < 0 || values.CostPack < 0)
            return Result.Failure<SeedAcquisition>(
                "Количество упаковок и стоимость должны быть неотрицательными и конечными");
        return LibraryName.Create(values.Culture).Bind(culture => LibraryName.Create(values.Sort).Bind(sort =>
            LibraryName.Create(values.Producer).Bind(producer => SeedAmount.Create(values.QuantityInPack, values.Unit)
                .Bind(_ =>
                    SeedAmount.Create(values.QuantityInPack * values.PackCount, values.Unit).Bind(total =>
                        Result.Success(new SeedAcquisition(values with { Culture = culture.Value, Sort = sort.Value, Producer = producer.Value }, total)))))));
    }

    public Result<SeedPackage> CreatePackage(DateTime purchasedAt)
        => SeedPackage.Create(new SeedPackageValues
        {
            WeightPack = Values.Unit == SeedUnit.Grams ? Values.QuantityInPack : 0,
            QuantityPack = Values.Unit == SeedUnit.Pieces ? Values.QuantityInPack : 0,
            AmountSeeds = Values.Unit == SeedUnit.Pieces ? Total.Value : 0,
            AmountSeedsWeight = Values.Unit == SeedUnit.Grams ? Option.Some(Total.Value) : Option.None<double>(),
            PurchaseDate = purchasedAt, ExpirationDate = Values.BestBy,
            SeedSource = Option.Some(Values.Source), CostPack = Values.CostPack, Note = Values.Note
        });

    public Result<SeedPackage> Replenish(SeedPackage package, DateTime purchasedAt)
    {
        ArgumentNullException.ThrowIfNull(package);
        var details = package.Values with
        {
            PurchaseDate = purchasedAt, Note = Values.Note, CostPack = Values.CostPack
        };
        return Values.Unit == SeedUnit.Pieces
            ? package.WithDetails(details with { AmountSeeds = details.AmountSeeds + Total.Value })
            : details.AmountSeedsWeight.Match(
                weight => package.WithDetails(details with { AmountSeedsWeight = Option.Some(weight + Total.Value) }),
                () => Result.Failure<SeedPackage>("Остаток в граммах не указан; задайте его в редакторе семян"));
    }
}