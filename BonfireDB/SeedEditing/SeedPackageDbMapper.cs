using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;
using BonfireDB.Entities;

namespace BonfireDB.SeedEditing;

public static class SeedPackageDbMapper
{
    public static SeedPackageValues Read(SeedsInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        return new()
        {
            WeightPack = info.WeightPack, QuantityPack = info.QuantityPack, AmountSeeds = info.AmountSeeds,
            AmountSeedsWeight = ToOption(info.AmountSeedsWeight), CostPack = info.CostPack,
            PurchaseDate = info.PurchaseDate, ExpirationDate = info.ExpirationDate,
            Note = ToOption(info.Note), SeedSource = ToOption(info.SeedSource), DisposeComment = ToOption(info.DisposeComment)
        };
    }

    public static void Apply(SeedPackageValues values, SeedsInfo target)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(target);
        target.WeightPack = values.WeightPack;
        target.QuantityPack = values.QuantityPack;
        target.AmountSeeds = values.AmountSeeds;
        target.AmountSeedsWeight = ToNullable(values.AmountSeedsWeight);
        target.CostPack = values.CostPack;
        target.PurchaseDate = values.PurchaseDate;
        target.ExpirationDate = values.ExpirationDate;
        target.Note = ToNullable(values.Note);
        target.SeedSource = ToNullable(values.SeedSource);
        target.DisposeComment = ToNullable(values.DisposeComment);
    }

    private static Option<T> ToOption<T>(T? value) where T : struct
        => value is { } present ? Option.Some(present) : Option.None<T>();
    private static Option<string> ToOption(string? value)
        => value is { } present ? Option.Some(present) : Option.None<string>();
    private static T? ToNullable<T>(Option<T> value) where T : struct => value.Match<T?>(present => present, () => null);
    private static string? ToNullable(Option<string> value) => value.Match<string?>(present => present, () => null);
}
