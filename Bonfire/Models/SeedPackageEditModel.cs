using System;
using System.Runtime.CompilerServices;
using Bonfire.Domain.Functional;
using Bonfire.Domain.Seeds;
using Bonfire.ViewModels.Base;

namespace Bonfire.Models;

public sealed class SeedPackageEditModel : ViewModel
{
    public SeedPackageValues Values { get; private set; }
    public bool HasErrors => SeedPackage.Create(Values).IsFailure;

    public SeedPackageEditModel(SeedPackageValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Values = values;
    }

    public double WeightPack
    {
        get => Values.WeightPack;
        set => Update(Values with { WeightPack = value });
    }

    public double QuantityPack
    {
        get => Values.QuantityPack;
        set => Update(Values with { QuantityPack = value });
    }

    public double AmountSeeds
    {
        get => Values.AmountSeeds;
        set => Update(Values with { AmountSeeds = value });
    }

    public double? AmountSeedsWeight
    {
        get => Values.AmountSeedsWeight.ToNullable();
        set => Update(Values with { AmountSeedsWeight = value.ToOption() });
    }

    public decimal CostPack
    {
        get => Values.CostPack;
        set => Update(Values with { CostPack = value });
    }

    public DateTime PurchaseDate
    {
        get => Values.PurchaseDate;
        set => Update(Values with { PurchaseDate = value });
    }

    public DateTime ExpirationDate
    {
        get => Values.ExpirationDate;
        set => Update(Values with { ExpirationDate = value });
    }

    public string? Note
    {
        get => Values.Note.ToNullable();
        set => Update(Values with { Note = value.ToOption() });
    }

    public string? SeedSource
    {
        get => Values.SeedSource.ToNullable();
        set => Update(Values with { SeedSource = value.ToOption() });
    }

    public string? DisposeComment
    {
        get => Values.DisposeComment.ToNullable();
        set => Update(Values with { DisposeComment = value.ToOption() });
    }

    private void Update(SeedPackageValues values, [CallerMemberName] string? property = null)
    {
        if (Values == values) return;
        Values = values;
        OnPropertyChanged(property);
        OnPropertyChanged(nameof(Values));
        OnPropertyChanged(nameof(HasErrors));
    }
}
