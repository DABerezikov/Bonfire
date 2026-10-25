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
            AmountSeedsWeight = info.AmountSeedsWeight.ToOption(), CostPack = info.CostPack,
            PurchaseDate = info.PurchaseDate, ExpirationDate = info.ExpirationDate,
            Note = info.Note.ToOption(), SeedSource = info.SeedSource.ToOption(), DisposeComment = info.DisposeComment.ToOption()
        };
    }

    public static void Apply(SeedPackageValues values, SeedsInfo target)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(target);
        target.WeightPack = values.WeightPack;
        target.QuantityPack = values.QuantityPack;
        target.AmountSeeds = values.AmountSeeds;
        target.AmountSeedsWeight = values.AmountSeedsWeight.ToNullable();
        target.CostPack = values.CostPack;
        target.PurchaseDate = values.PurchaseDate;
        target.ExpirationDate = values.ExpirationDate;
        target.Note = values.Note.ToNullable();
        target.SeedSource = values.SeedSource.ToNullable();
        target.DisposeComment = values.DisposeComment.ToNullable();
    }
}
